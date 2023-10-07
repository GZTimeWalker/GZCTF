using GZCTF.Models.Internal;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace GZCTF.Repositories;

public class GameInstanceRepository(AppDbContext context,
    IContainerManager service,
    ICheatInfoRepository cheatInfoRepository,
    IContainerRepository containerRepository,
    IGameEventRepository gameEventRepository,
    IOptionsSnapshot<ContainerPolicy> containerPolicy,
    ILogger<GameInstanceRepository> logger,
    IStringLocalizer<Program> localizer) : RepositoryBase(context), IGameInstanceRepository
{
    public async Task<GameInstance?> GetInstance(Participation part, int challengeId, CancellationToken token = default)
    {
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(token);

        GameInstance? instance = await context.GameInstances
            .Include(i => i.FlagContext)
            .Where(e => e.ChallengeId == challengeId && e.Participation == part)
            .SingleOrDefaultAsync(token);

        if (instance is null)
        {
            logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.InstanceRepository_NoInstance), part.Id, challengeId], TaskStatus.NotFound,
                LogLevel.Warning);
            return null;
        }

        if (instance.IsLoaded)
        {
            await transaction.CommitAsync(token);
            return instance;
        }

        var challenge = instance.Challenge;

        if (challenge is null || !challenge.IsEnabled)
        {
            await transaction.CommitAsync(token);
            return null;
        }

        try
        {
            // dynamic flag dispatch
            switch (instance.Challenge.Type)
            {
                case ChallengeType.DynamicContainer:
                    instance.FlagContext = new()
                    {
                        Challenge = challenge,
                        // tiny probability will produce the same FLAG,
                        // but this will not affect the correctness of the answer
                        Flag = challenge.GenerateDynamicFlag(part),
                        IsOccupied = true
                    };
                    break;
                case ChallengeType.DynamicAttachment:
                    List<FlagContext> flags = await context.FlagContexts
                        .Where(e => e.Challenge == challenge && !e.IsOccupied)
                        .ToListAsync(token);

                    if (flags.Count == 0)
                    {
                        logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.InstanceRepository_DynamicFlagsNotEnough), challenge.Title, challenge.Id], TaskStatus.Failed,
                            LogLevel.Warning);
                        return null;
                    }

                    var pos = Random.Shared.Next(flags.Count);
                    flags[pos].IsOccupied = true;

                    instance.FlagId = flags[pos].Id;
                    break;
            }

            // instance.FlagContext is null by default
            // static flag does not need to be dispatched

            instance.IsLoaded = true;
            await SaveAsync(token);
            await transaction.CommitAsync(token);
        }
        catch
        {
            logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.InstanceRepository_GetInstanceFailed), part.Team.Name, challenge.Title, challenge.Id],
                TaskStatus.Failed, LogLevel.Warning);
            await transaction.RollbackAsync(token);
            return null;
        }

        return instance;
    }

    public async Task<TaskResult<Container>> CreateContainer(GameInstance gameInstance, Team team, UserInfo user,
        int containerLimit = 3, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(gameInstance.Challenge.ContainerImage) || gameInstance.Challenge.ContainerExposePort is null)
        {
            logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.InstanceRepository_ContainerCreationFailed), gameInstance.Challenge.Title], TaskStatus.Denied, LogLevel.Warning);
            return new TaskResult<Container>(TaskStatus.Failed);
        }

        // containerLimit == 0 means unlimited
        if (containerLimit > 0)
        {
            if (containerPolicy.Value.AutoDestroyOnLimitReached)
            {
                List<GameInstance> running = await context.GameInstances
                    .Where(i => i.Participation == gameInstance.Participation && i.Container != null)
                    .OrderBy(i => i.Container!.StartedAt).ToListAsync(token);

                GameInstance? first = running.FirstOrDefault();
                if (running.Count >= containerLimit && first is not null)
                {
                    logger.Log(Program.StaticLocalizer[nameof(Resources.Program.InstanceRepository_ContainerAutoDestroy), team.Name, first.Challenge.Title, first.Container!.ContainerId],
                        user, TaskStatus.Success);
                    await containerRepository.DestroyContainer(running.First().Container!, token);
                }
            }
            else
            {
                var count = await context.GameInstances.CountAsync(
                    i => i.Participation == gameInstance.Participation &&
                         i.Container != null, token);

                if (count >= containerLimit)
                    return new TaskResult<Container>(TaskStatus.Denied);
            }
        }

        if (gameInstance.Container is not null)
            return new TaskResult<Container>(TaskStatus.Success, gameInstance.Container);

        await context.Entry(gameInstance).Reference(e => e.FlagContext).LoadAsync(token);
        Container? container = await service.CreateContainerAsync(new ContainerConfig
        {
            TeamId = team.Id.ToString(),
            UserId = user.Id,
            Flag = gameInstance.FlagContext?.Flag, // static challenge has no specific flag
            Image = gameInstance.Challenge.ContainerImage,
            CPUCount = gameInstance.Challenge.CPUCount ?? 1,
            MemoryLimit = gameInstance.Challenge.MemoryLimit ?? 64,
            StorageLimit = gameInstance.Challenge.StorageLimit ?? 256,
            EnableTrafficCapture = gameInstance.Challenge.EnableTrafficCapture,
            ExposedPort = gameInstance.Challenge.ContainerExposePort ?? throw new ArgumentException(localizer[nameof(Resources.Program.InstanceRepository_InvalidPort)])
        }, token);

        if (container is null)
        {
            logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.InstanceRepository_ContainerCreationFailed), gameInstance.Challenge.Title], TaskStatus.Failed, LogLevel.Warning);
            return new TaskResult<Container>(TaskStatus.Failed);
        }

        gameInstance.Container = container;
        gameInstance.LastContainerOperation = DateTimeOffset.UtcNow;

        logger.Log(Program.StaticLocalizer[nameof(Resources.Program.InstanceRepository_ContainerCreated), team.Name, gameInstance.Challenge.Title, container.ContainerId], user,
            TaskStatus.Success);

        // will save instance together
        await gameEventRepository.AddEvent(
            new()
            {
                Type = EventType.ContainerStart,
                GameId = gameInstance.Challenge.GameId,
                TeamId = gameInstance.Participation.TeamId,
                UserId = user.Id,
                Content = localizer[nameof(Resources.Program.InstanceRepository_ContainerCreationEvent), gameInstance.Challenge.Title, gameInstance.Challenge.Id]
            }, token);

        return new TaskResult<Container>(TaskStatus.Success, gameInstance.Container);
    }

    public async Task ProlongContainer(Container container, TimeSpan time, CancellationToken token = default)
    {
        container.ExpectStopAt += time;
        await SaveAsync(token);
    }

    public Task<GameInstance[]> GetInstances(GameChallenge challenge, CancellationToken token = default) =>
        context.GameInstances.Where(i => i.Challenge == challenge).OrderBy(i => i.ParticipationId)
            .Include(i => i.Participation).ThenInclude(i => i.Team).ToArrayAsync(token);

    public async Task<CheatCheckInfo> CheckCheat(Submission submission, CancellationToken token = default)
    {
        CheatCheckInfo checkInfo = new();

        GameInstance[] instances = await context.GameInstances.Where(i => i.ChallengeId == submission.ChallengeId &&
                                                                  i.ParticipationId != submission.ParticipationId)
            .Include(i => i.FlagContext).Include(i => i.Participation)
            .ThenInclude(i => i.Team).ToArrayAsync(token);

        foreach (GameInstance instance in instances)
            if (instance.FlagContext?.Flag == submission.Answer)
            {
                Submission updateSub = await context.Submissions.Where(s => s.Id == submission.Id).SingleAsync(token);

                CheatInfo cheatInfo = await cheatInfoRepository.CreateCheatInfo(updateSub, instance, token);

                checkInfo = CheatCheckInfo.FromCheatInfo(cheatInfo);

                updateSub.Status = AnswerResult.CheatDetected;

                await SaveAsync(token);

                return checkInfo;
            }

        return checkInfo;
    }

    public async Task<VerifyResult> VerifyAnswer(Submission submission, CancellationToken token = default)
    {
        IDbContextTransaction trans = await context.Database.BeginTransactionAsync(token);

        try
        {
            GameInstance? instance = await context.GameInstances.IgnoreAutoIncludes().Include(i => i.FlagContext)
                .SingleOrDefaultAsync(i => i.ChallengeId == submission.ChallengeId &&
                                           i.ParticipationId == submission.ParticipationId, token);

            SubmissionType ret;

            if (instance is null)
            {
                submission.Status = AnswerResult.NotFound;
                return new(SubmissionType.Unaccepted, AnswerResult.NotFound);
            }

            // submission is from the queue, do not modify it directly
            // we need to fetch the entity again to ensure it is being tracked correctly
            Submission updateSub = await context.Submissions.SingleAsync(s => s.Id == submission.Id, token);

            if (instance.FlagContext is null && submission.GameChallenge.Type.IsStatic())
                updateSub.Status = await context.FlagContexts
                    .AsNoTracking()
                    .AnyAsync(
                        f => f.ChallengeId == submission.ChallengeId && f.Flag == submission.Answer,
                        token)
                    ? AnswerResult.Accepted
                    : AnswerResult.WrongAnswer;
            else
                updateSub.Status = instance.FlagContext?.Flag == submission.Answer
                    ? AnswerResult.Accepted
                    : AnswerResult.WrongAnswer;

            var firstTime = !instance.IsSolved && updateSub.Status == AnswerResult.Accepted;
            var beforeEnd = submission.Game.EndTimeUtc > submission.SubmitTimeUtc;

            updateSub.GameChallenge.SubmissionCount++;

            if (firstTime && beforeEnd)
            {
                instance.IsSolved = true;
                updateSub.GameChallenge.AcceptedCount++;
                ret = updateSub.GameChallenge.AcceptedCount switch
                {
                    1 => SubmissionType.FirstBlood,
                    2 => SubmissionType.SecondBlood,
                    3 => SubmissionType.ThirdBlood,
                    _ => SubmissionType.Normal
                };
            }
            else
            {
                ret = updateSub.Status == AnswerResult.Accepted ? SubmissionType.Normal : SubmissionType.Unaccepted;
            }

            await SaveAsync(token);
            await trans.CommitAsync(token);

            return new(ret, updateSub.Status);
        }
        catch
        {
            await trans.RollbackAsync(token);
            throw;
        }
    }
}
