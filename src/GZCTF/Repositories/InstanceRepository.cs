using GZCTF.Models.Internal;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace GZCTF.Repositories;

public class InstanceRepository(AppDbContext context,
    IContainerManager service,
    ICheatInfoRepository cheatInfoRepository,
    IContainerRepository containerRepository,
    IGameEventRepository gameEventRepository,
    IOptionsSnapshot<GamePolicy> gamePolicy,
    ILogger<InstanceRepository> logger) : RepositoryBase(context), IInstanceRepository
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
            logger.SystemLog($"队伍对应参与对象为空，这可能是非预期的情况 [{part.Id}, {challengeId}]", TaskStatus.NotFound,
                LogLevel.Warning);
            return null;
        }

        if (instance.IsLoaded)
        {
            await transaction.CommitAsync(token);
            return instance;
        }

        if (instance.GameChallenge.IsEnabled)
        {
            await transaction.CommitAsync(token);
            return null;
        }

        var challenge = instance.GameChallenge;

        try
        {
            // dynamic flag dispatch
            switch (instance.GameChallenge.Type)
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
                        logger.SystemLog($"题目 {challenge.Title}#{challenge.Id} 请求分配的动态附件数量不足", TaskStatus.Failed,
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
            logger.SystemLog($"为队伍 {part.Team.Name} 获取题目 {challenge.Title}#{challenge.Id} 的实例时遇到问题（可能由于并发错误），回滚中",
                TaskStatus.Failed, LogLevel.Warning);
            await transaction.RollbackAsync(token);
            return null;
        }

        return instance;
    }

    public async Task<bool> DestroyContainer(Container container, CancellationToken token = default)
    {
        try
        {
            await service.DestroyContainerAsync(container, token);
            await containerRepository.RemoveContainer(container, token);
            return true;
        }
        catch (Exception ex)
        {
            logger.SystemLog(
                $"销毁容器 [{container.ContainerId[..12]}] ({container.Image.Split("/").LastOrDefault()}): {ex.Message}",
                TaskStatus.Failed, LogLevel.Warning);
            return false;
        }
    }

    public async Task<TaskResult<Container>> CreateContainer(GameInstance gameInstance, Team team, UserInfo user,
        int containerLimit = 3, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(gameInstance.GameChallenge.ContainerImage) || gameInstance.GameChallenge.ContainerExposePort is null)
        {
            logger.SystemLog($"无法为题目 {gameInstance.GameChallenge.Title} 启动容器实例", TaskStatus.Denied, LogLevel.Warning);
            return new TaskResult<Container>(TaskStatus.Failed);
        }

        // containerLimit == 0 means unlimited
        if (containerLimit > 0)
        {
            if (gamePolicy.Value.AutoDestroyOnLimitReached)
            {
                List<GameInstance> running = await context.GameInstances
                    .Where(i => i.Participation == gameInstance.Participation && i.Container != null)
                    .OrderBy(i => i.Container!.StartedAt).ToListAsync(token);

                GameInstance? first = running.FirstOrDefault();
                if (running.Count >= containerLimit && first is not null)
                {
                    logger.Log($"{team.Name} 自动销毁题目 {first.GameChallenge.Title} 的容器实例 [{first.Container!.ContainerId}]",
                        user, TaskStatus.Success);
                    await DestroyContainer(running.First().Container!, token);
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
            Image = gameInstance.GameChallenge.ContainerImage,
            CPUCount = gameInstance.GameChallenge.CPUCount ?? 1,
            MemoryLimit = gameInstance.GameChallenge.MemoryLimit ?? 64,
            StorageLimit = gameInstance.GameChallenge.StorageLimit ?? 256,
            EnableTrafficCapture = gameInstance.GameChallenge.EnableTrafficCapture,
            ExposedPort = gameInstance.GameChallenge.ContainerExposePort ?? throw new ArgumentException("创建容器时遇到无效的端口")
        }, token);

        if (container is null)
        {
            logger.SystemLog($"为题目 {gameInstance.GameChallenge.Title} 启动容器实例失败", TaskStatus.Failed, LogLevel.Warning);
            return new TaskResult<Container>(TaskStatus.Failed);
        }

        gameInstance.Container = container;
        gameInstance.LastContainerOperation = DateTimeOffset.UtcNow;

        logger.Log($"{team.Name} 启动题目 {gameInstance.GameChallenge.Title} 的容器实例 [{container.ContainerId}]", user,
            TaskStatus.Success);

        // will save instance together
        await gameEventRepository.AddEvent(
            new()
            {
                Type = EventType.ContainerStart,
                GameId = gameInstance.GameChallenge.GameId,
                TeamId = gameInstance.Participation.TeamId,
                UserId = user.Id,
                Content = $"{gameInstance.GameChallenge.Title}#{gameInstance.GameChallenge.Id} 启动容器实例"
            }, token);

        return new TaskResult<Container>(TaskStatus.Success, gameInstance.Container);
    }

    public async Task ProlongContainer(Container container, TimeSpan time, CancellationToken token = default)
    {
        container.ExpectStopAt += time;
        await SaveAsync(token);
    }

    public Task<GameInstance[]> GetInstances(GameChallenge gameChallenge, CancellationToken token = default) =>
        context.GameInstances.Where(i => i.GameChallenge == gameChallenge).OrderBy(i => i.ParticipationId)
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