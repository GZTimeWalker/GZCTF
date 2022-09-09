using CTFServer.Models;
using CTFServer.Models.Internal;
using CTFServer.Repositories.Interface;
using CTFServer.Services.Interface;
using CTFServer.Utils;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Repositories;

public class InstanceRepository : RepositoryBase, IInstanceRepository
{
    private readonly IContainerService service;
    private readonly IContainerRepository containerRepository;
    private readonly IGameEventRepository gameEventRepository;
    private readonly ILogger<InstanceRepository> logger;

    public InstanceRepository(AppDbContext _context,
        IContainerService _service,
        IContainerRepository _containerRepository,
        IGameEventRepository _gameEventRepository,
        ILogger<InstanceRepository> _logger) : base(_context)
    {
        logger = _logger;
        service = _service;
        gameEventRepository = _gameEventRepository;
        containerRepository = _containerRepository;
    }

    public async Task<Instance?> GetInstance(Participation part, int challengeId, CancellationToken token = default)
    {
        var instance = await context.Instances
            .Include(i => i.FlagContext)
            .Where(e => e.ChallengeId == challengeId && e.Participation == part)
            .SingleOrDefaultAsync(token);

        if (instance is null)
        {
            logger.SystemLog($"队伍对应参与对象为空，这可能是非预期的情况 [{part.Id}, {challengeId}]", TaskStatus.NotFound, LogLevel.Warning);
            return null;
        }

        if (instance.IsLoaded)
            return instance;

        var challenge = instance.Challenge;

        if (challenge is null || !challenge.IsEnabled)
            return null;

        if (challenge.Type.IsStatic())
            instance.FlagContext = null; // use challenge to verify
        else
        {
            if (challenge.Type.IsAttachment())
            {
                var flags = await context.FlagContexts
                    .Where(e => e.Challenge == challenge && !e.IsOccupied)
                    .ToListAsync(token);

                if (flags.Count == 0)
                {
                    logger.SystemLog($"题目 {challenge.Title}#{challenge.Id} 请求分配的动态附件数量不足", TaskStatus.Fail, LogLevel.Warning);
                    return null;
                }

                var pos = Random.Shared.Next(flags.Count);
                flags[pos].IsOccupied = true;

                instance.FlagId = flags[pos].Id;
            }
            else
            {
                instance.FlagContext = new()
                {
                    Challenge = challenge,
                    Flag = challenge.GenerateFlag(part),
                    IsOccupied = true
                };
            }
        }

        instance.IsLoaded = true;

        await SaveAsync(token);

        return instance;
    }

    public async Task<bool> DestoryContainer(Container container, CancellationToken token = default)
    {
        try
        {
            await service.DestoryContainer(container, token);
            await containerRepository.RemoveContainer(container, token);
            return true;
        }
        catch (Exception ex)
        {
            logger.SystemLog($"销毁容器 [{container.ContainerId[..12]}] ({container.Image.Split("/").LastOrDefault()}): {ex.Message}", TaskStatus.Fail, LogLevel.Warning);
            return false;
        }
    }

    public async Task<TaskResult<Container>> CreateContainer(Instance instance, Team team, string userId, int containerLimit = 3, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(instance.Challenge.ContainerImage) || instance.Challenge.ContainerExposePort is null)
        {
            logger.SystemLog($"无法为题目 {instance.Challenge.Title} 启动容器实例", TaskStatus.Denied, LogLevel.Warning);
            return new TaskResult<Container>(TaskStatus.Fail);
        }

        if (await context.Instances.CountAsync(i => i.Participation == instance.Participation
                && i.Container != null
                && i.Container.Status == ContainerStatus.Running, token) >= containerLimit)
            return new TaskResult<Container>(TaskStatus.Denied);

        if (instance.Container is null)
        {
            await context.Entry(instance).Reference(e => e.FlagContext).LoadAsync(token);
            var container = await service.CreateContainer(new ContainerConfig()
            {
                CPUCount = instance.Challenge.CPUCount ?? 1,
                TeamInfo = $"{team.Name}:{team.Id}:{userId}",
                Flag = instance.FlagContext?.Flag, // static challenge has no specific flag
                Image = instance.Challenge.ContainerImage,
                MemoryLimit = instance.Challenge.MemoryLimit ?? 64,
                ExposedPort = instance.Challenge.ContainerExposePort ?? throw new ArgumentException("创建容器时遇到无效的端口"),
            }, token);

            if (container is null)
            {
                logger.SystemLog($"为题目 {instance.Challenge.Title} 启动容器实例失败", TaskStatus.Fail, LogLevel.Warning);
                return new TaskResult<Container>(TaskStatus.Fail);
            }

            instance.Container = container;
            instance.LastContainerOperation = DateTimeOffset.UtcNow;

            // will save instance together
            await gameEventRepository.AddEvent(new()
            {
                Type = EventType.ContainerStart,
                GameId = instance.Challenge.GameId,
                TeamId = instance.Participation.TeamId,
                UserId = userId,
                Content = $"{instance.Challenge.Title}#{instance.Challenge.Id} 启动容器实例"
            }, token);
        }

        return new TaskResult<Container>(TaskStatus.Success, instance.Container);
    }

    public async Task ProlongContainer(Container container, TimeSpan time, CancellationToken token = default)
    {
        container.ExpectStopAt += time;
        await SaveAsync(token);
    }

    public Task<Instance[]> GetInstances(Challenge challenge, CancellationToken token = default)
        => context.Instances.Where(i => i.Challenge == challenge).OrderBy(i => i.ParticipationId)
            .Include(i => i.Participation).ThenInclude(i => i.Team).ToArrayAsync(token);

    public async Task<CheatCheckInfo> CheckCheat(Submission submission, CancellationToken token = default)
    {
        CheatCheckInfo checkInfo = new();

        var instances = await context.Instances.Where(i => i.ChallengeId == submission.ChallengeId &&
                i.ParticipationId != submission.ParticipationId)
                .Include(i => i.Challenge).Include(i => i.FlagContext)
                .Include(i => i.Participation).ThenInclude(i => i.Team).ToArrayAsync(token);

        foreach (var instance in instances)
        {
            if (instance.FlagContext?.Flag == submission.Answer)
            {
                checkInfo.AnswerResult = AnswerResult.CheatDetected;
                checkInfo.CheatUser = submission.User;
                checkInfo.CheatTeam = submission.Team;
                checkInfo.SourceTeam = instance.Participation.Team;
                checkInfo.Challenge = instance.Challenge;

                var updateSub = await context.Submissions.Where(s => s.Id == submission.Id).SingleAsync(token);

                if (updateSub is not null)
                    updateSub.Status = AnswerResult.CheatDetected;

                await SaveAsync(token);

                return checkInfo;
            }
        }

        return checkInfo;
    }

    public async Task<(SubmissionType, AnswerResult)> VerifyAnswer(Submission submission, CancellationToken token = default)
    {
        var instance = await context.Instances
            .IgnoreAutoIncludes()
            .Include(i => i.FlagContext)
            .SingleOrDefaultAsync(i => i.ChallengeId == submission.ChallengeId &&
                i.ParticipationId == submission.ParticipationId, token);

        // submission is from the queue, do not modify it directly
        // we need to requery the entity to ensure it is being tracked correctly
        var updateSub = await context.Submissions
                .Include(s => s.Challenge)
                .SingleAsync(s => s.Id == submission.Id, token);

        var ret = SubmissionType.Unaccepted;

        if (instance is null)
        {
            submission.Status = AnswerResult.NotFound;
            return (SubmissionType.Unaccepted, AnswerResult.NotFound);
        }
        else if (instance.FlagContext is null && submission.Challenge.Type.IsStatic())
        {
            updateSub.Status = await context.FlagContexts
                .AsNoTracking()
                .AnyAsync(
                    f => f.ChallengeId == submission.ChallengeId && f.Flag == submission.Answer,
                    token)
                ? AnswerResult.Accepted : AnswerResult.WrongAnswer;
        }
        else
        {
            updateSub.Status = instance.FlagContext?.Flag == submission.Answer
                ? AnswerResult.Accepted : AnswerResult.WrongAnswer;
        }

        bool firstTime = !instance.IsSolved && updateSub.Status == AnswerResult.Accepted;

        if (firstTime)
        {
            instance.IsSolved = true;
            updateSub.Challenge.AcceptedCount++;
            ret = updateSub.Challenge.AcceptedCount switch
            {
                1 => SubmissionType.FirstBlood,
                2 => SubmissionType.SecondBlood,
                3 => SubmissionType.ThirdBlood,
                _ => SubmissionType.Normal
            };
        }
        else
        {
            ret = updateSub.Status == AnswerResult.Accepted ?
                SubmissionType.Normal : SubmissionType.Unaccepted;
        }

        await SaveAsync(token);

        return (ret, updateSub.Status);
    }
}