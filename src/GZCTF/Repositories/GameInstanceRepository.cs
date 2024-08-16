using GZCTF.Models.Internal;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Container.Manager;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace GZCTF.Repositories;

public class GameInstanceRepository(
    AppDbContext context,
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
        await using IDbContextTransaction transaction = await Context.Database.BeginTransactionAsync(token);

        GameInstance? instance = await Context.GameInstances
            .Include(i => i.FlagContext)
            .Where(e => e.ChallengeId == challengeId && e.Participation == part)
            .SingleOrDefaultAsync(token);

        if (instance is null)
        {
            logger.SystemLog(
                Program.StaticLocalizer[nameof(Resources.Program.InstanceRepository_NoInstance), part.Id, challengeId],
                TaskStatus.NotFound,
                LogLevel.Warning);
            return null;
        }

        GameChallenge challenge = instance.Challenge;

        if (!challenge.IsEnabled)
        {
            await transaction.CommitAsync(token);
            return null;
        }

        if (instance.IsLoaded)
        {
            await transaction.CommitAsync(token);
            return instance;
        }

        try
        {
            switch (instance.Challenge.Type)
            {
                // dynamic flag dispatch
                case ChallengeType.DynamicContainer:
                    instance.FlagContext = new()
                    {
                        Challenge = challenge,
                        Flag
                            // tiny probability will produce the same FLAG,
                            // but this will not affect the correctness of the answer
                            = challenge.GenerateDynamicFlag(part),
                        IsOccupied = true
                    };
                    break;
                case ChallengeType.DynamicAttachment:
                    var flags = await Context.FlagContexts
                        .Where(e => e.Challenge == challenge && !e.IsOccupied)
                        .ToListAsync(token);

                    if (flags.Count == 0)
                    {
                        logger.SystemLog(
                            Program.StaticLocalizer[nameof(Resources.Program.InstanceRepository_DynamicFlagsNotEnough),
                                challenge.Title,
                                challenge.Id], TaskStatus.Failed,
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
            logger.SystemLog(
                Program.StaticLocalizer[nameof(Resources.Program.InstanceRepository_GetInstanceFailed), part.Team.Name,
                    challenge.Title,
                    challenge.Id],
                TaskStatus.Failed, LogLevel.Warning);
            await transaction.RollbackAsync(token);
            return null;
        }

        return instance;
    }

    public async Task<TaskResult<Container>> CreateContainer(GameInstance gameInstance, Team team, UserInfo user,
        Game game, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(gameInstance.Challenge.ContainerImage) ||
            gameInstance.Challenge.ContainerExposePort is null)
        {
            logger.SystemLog(
                Program.StaticLocalizer[nameof(Resources.Program.InstanceRepository_ContainerCreationFailed),
                    gameInstance.Challenge.Title],
                TaskStatus.Denied, LogLevel.Warning);
            return new TaskResult<Container>(TaskStatus.Failed);
        }

        // containerLimit == 0 means unlimited
        if (game.ContainerCountLimit > 0)
        {
            if (containerPolicy.Value.AutoDestroyOnLimitReached)
            {
                List<GameInstance> running = await Context.GameInstances
                    .Where(i => i.Participation == gameInstance.Participation && i.Container != null)
                    .OrderBy(i => i.Container!.StartedAt).ToListAsync(token);

                GameInstance? first = running.FirstOrDefault();
                if (running.Count >= game.ContainerCountLimit && first is not null)
                {
                    logger.Log(
                        Program.StaticLocalizer[nameof(Resources.Program.InstanceRepository_ContainerAutoDestroy),
                            team.Name, first.Challenge.Title,
                            first.Container!.ContainerId],
                        user, TaskStatus.Success);
                    await containerRepository.DestroyContainer(running.First().Container!, token);
                }
            }
            else
            {
                var count = await Context.GameInstances.CountAsync(
                    i => i.Participation == gameInstance.Participation &&
                         i.Container != null, token);

                if (count >= game.ContainerCountLimit)
                    return new TaskResult<Container>(TaskStatus.Denied);
            }
        }

        if (gameInstance.Container is not null)
            return new TaskResult<Container>(TaskStatus.Success, gameInstance.Container);

        await Context.Entry(gameInstance).Reference(e => e.FlagContext).LoadAsync(token);
        Container? container = await service.CreateContainerAsync(new ContainerConfig
        {
            TeamId = team.Id.ToString(),
            UserId = user.Id,
            ChallengeId = gameInstance.ChallengeId,
            Flag = gameInstance.FlagContext?.Flag, // static challenge has no specific flag
            Image = gameInstance.Challenge.ContainerImage,
            CPUCount = gameInstance.Challenge.CPUCount ?? 1,
            MemoryLimit = gameInstance.Challenge.MemoryLimit ?? 64,
            StorageLimit = gameInstance.Challenge.StorageLimit ?? 256,
            EnableTrafficCapture = gameInstance.Challenge.EnableTrafficCapture && game.IsActive,
            ExposedPort = gameInstance.Challenge.ContainerExposePort ??
                          throw new ArgumentException(
                              localizer[nameof(Resources.Program.InstanceRepository_InvalidPort)])
        }, token);

        if (container is null)
        {
            logger.SystemLog(
                Program.StaticLocalizer[nameof(Resources.Program.InstanceRepository_ContainerCreationFailed),
                    gameInstance.Challenge.Title],
                TaskStatus.Failed, LogLevel.Warning);
            return new TaskResult<Container>(TaskStatus.Failed);
        }

        // update the ExpectStopAt with config
        container.ExpectStopAt = container.StartedAt.AddMinutes(containerPolicy.Value.DefaultLifetime);

        gameInstance.Container = container;
        gameInstance.LastContainerOperation = DateTimeOffset.UtcNow;

        logger.Log(
            Program.StaticLocalizer[nameof(Resources.Program.InstanceRepository_ContainerCreated), team.Name,
                gameInstance.Challenge.Title,
                container.ContainerId], user,
            TaskStatus.Success);

        // will save instance together
        await gameEventRepository.AddEvent(
            new()
            {
                Type = EventType.ContainerStart,
                GameId = gameInstance.Challenge.GameId,
                TeamId = gameInstance.Participation.TeamId,
                UserId = user.Id,
                Values = [gameInstance.Challenge.Id.ToString(), gameInstance.Challenge.Title]
            }, token);

        return new TaskResult<Container>(TaskStatus.Success, gameInstance.Container);
    }

    public async Task DestroyAllInstances(GameChallenge challenge, CancellationToken token = default)
    {
        foreach (Container? container in await Context.GameInstances
                     .Include(i => i.Container)
                     .Where(i => i.Challenge == challenge && i.ContainerId != null)
                     .Select(i => i.Container)
                     .ToArrayAsync(token))
        {
            if (container is null)
                continue;

            await containerRepository.DestroyContainer(container, token);
        }
    }

    public async Task<CheatCheckInfo> CheckCheat(Submission submission, CancellationToken token = default)
    {
        CheatCheckInfo checkInfo = new();

        GameInstance? instance = await Context.GameInstances
            .Include(i => i.Participation)
            .ThenInclude(i => i.Team)
            .Include(i => i.FlagContext)
            .Where(i => i.ChallengeId == submission.ChallengeId &&
                        i.ParticipationId != submission.ParticipationId &&
                        i.FlagContext != null && i.FlagContext.Flag == submission.Answer)
            .FirstOrDefaultAsync(token);

        if (instance is null)
            return checkInfo;

        Submission updateSub = await Context.Submissions.Where(s => s.Id == submission.Id).SingleAsync(token);

        CheatInfo cheatInfo = await cheatInfoRepository.CreateCheatInfo(updateSub, instance, token);

        checkInfo = CheatCheckInfo.FromCheatInfo(cheatInfo);

        updateSub.Status = AnswerResult.CheatDetected;

        await SaveAsync(token);

        return checkInfo;
    }

    public async Task<VerifyResult> VerifyAnswer(Submission submission, CancellationToken token = default)
    {
        IDbContextTransaction trans = await Context.Database.BeginTransactionAsync(token);

        try
        {
            GameInstance? instance = await Context.GameInstances.IgnoreAutoIncludes()
                .Include(i => i.FlagContext)
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
            Submission updateSub = await Context.Submissions.SingleAsync(s => s.Id == submission.Id, token);

            if (instance.FlagContext is null && submission.GameChallenge.Type.IsStatic())
                updateSub.Status = await Context.FlagContexts.AsNoTracking()
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

    public Task<GameInstance[]> GetInstances(GameChallenge challenge, CancellationToken token = default) =>
        Context.GameInstances.Where(i => i.Challenge == challenge).OrderBy(i => i.ParticipationId)
            .Include(i => i.Participation).ThenInclude(i => i.Team).ToArrayAsync(token);
}
