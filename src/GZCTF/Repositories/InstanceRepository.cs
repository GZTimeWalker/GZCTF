using GZCTF.Models.Internal;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Interface;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GZCTF.Repositories;

public class InstanceRepository : RepositoryBase, IInstanceRepository
{
    private readonly IContainerService _service;
    private readonly ICheatInfoRepository _cheatInfoRepository;
    private readonly IContainerRepository _containerRepository;
    private readonly IGameEventRepository _gameEventRepository;
    private readonly ILogger<InstanceRepository> _logger;
    private readonly IOptionsSnapshot<GamePolicy> _gamePolicy;

    public InstanceRepository(AppDbContext context,
        IContainerService service,
        ICheatInfoRepository cheatInfoRepository,
        IContainerRepository containerRepository,
        IGameEventRepository gameEventRepository,
        IOptionsSnapshot<GamePolicy> gamePolicy,
        ILogger<InstanceRepository> logger) : base(context)
    {
        _logger = logger;
        _service = service;
        _gamePolicy = gamePolicy;
        _cheatInfoRepository = cheatInfoRepository;
        _gameEventRepository = gameEventRepository;
        _containerRepository = containerRepository;
    }

    public async Task<Instance?> GetInstance(Participation part, int challengeId, CancellationToken token = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(token);

        var instance = await _context.Instances
            .Include(i => i.FlagContext)
            .Where(e => e.ChallengeId == challengeId && e.Participation == part)
            .SingleOrDefaultAsync(token);

        if (instance is null)
        {
            _logger.SystemLog($"队伍对应参与对象为空，这可能是非预期的情况 [{part.Id}, {challengeId}]", TaskStatus.NotFound, LogLevel.Warning);
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
            switch (challenge.Type)
            {
                case ChallengeType.DynamicContainer:
                    instance.FlagContext = new()
                    {
                        Challenge = challenge,
                        // tiny probability will produce the same FLAG,
                        // but this will not affect the correctness of the answer
                        Flag = challenge.GenerateFlag(part),
                        IsOccupied = true
                    };
                    break;
                case ChallengeType.DynamicAttachment:
                    var flags = await _context.FlagContexts
                        .Where(e => e.Challenge == challenge && !e.IsOccupied)
                        .ToListAsync(token);

                    if (flags.Count == 0)
                    {
                        _logger.SystemLog($"题目 {challenge.Title}#{challenge.Id} 请求分配的动态附件数量不足", TaskStatus.Failed, LogLevel.Warning);
                        return null;
                    }

                    var pos = Random.Shared.Next(flags.Count);
                    flags[pos].IsOccupied = true;

                    instance.FlagId = flags[pos].Id;
                    break;
                default:
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
            _logger.SystemLog($"为队伍 {part.Team.Name} 获取题目 {challenge.Title}#{challenge.Id} 的实例时遇到问题（可能由于并发错误），回滚中", TaskStatus.Failed, LogLevel.Warning);
            await transaction.RollbackAsync(token);
            return null;
        }

        return instance;
    }

    public async Task<bool> DestroyContainer(Container container, CancellationToken token = default)
    {
        try
        {
            await _service.DestroyContainerAsync(container, token);
            await _containerRepository.RemoveContainer(container, token);
            return true;
        }
        catch (Exception ex)
        {
            _logger.SystemLog($"销毁容器 [{container.ContainerId[..12]}] ({container.Image.Split("/").LastOrDefault()}): {ex.Message}", TaskStatus.Failed, LogLevel.Warning);
            return false;
        }
    }

    public async Task<TaskResult<Container>> CreateContainer(Instance instance, Team team, UserInfo user, int containerLimit = 3, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(instance.Challenge.ContainerImage) || instance.Challenge.ContainerExposePort is null)
        {
            _logger.SystemLog($"无法为题目 {instance.Challenge.Title} 启动容器实例", TaskStatus.Denied, LogLevel.Warning);
            return new TaskResult<Container>(TaskStatus.Failed);
        }

        // containerLimit == 0 means unlimit
        if (containerLimit > 0)
        {
            if (_gamePolicy.Value.AutoDestroyOnLimitReached)
            {
                var running = await _context.Instances
                    .Where(i => i.Participation == instance.Participation && i.Container != null)
                    .OrderBy(i => i.Container!.StartedAt).ToListAsync(token);

                var first = running.FirstOrDefault();
                if (running.Count >= containerLimit && first is not null)
                {
                    _logger.Log($"{team.Name} 自动销毁题目 {first.Challenge.Title} 的容器实例 [{first.Container!.ContainerId}]", user, TaskStatus.Success);
                    await DestroyContainer(running.First().Container!, token);
                }
            }
            else
            {
                var count = await _context.Instances.CountAsync(
                    i => i.Participation == instance.Participation &&
                    i.Container != null, token);

                if (count >= containerLimit)
                    return new TaskResult<Container>(TaskStatus.Denied);
            }
        }

        if (instance.Container is null)
        {
            await _context.Entry(instance).Reference(e => e.FlagContext).LoadAsync(token);
            var container = await _service.CreateContainerAsync(new ContainerConfig()
            {
                TeamId = team.Id.ToString(),
                UserId = user.Id,
                Flag = instance.FlagContext?.Flag, // static challenge has no specific flag
                Image = instance.Challenge.ContainerImage,
                CPUCount = instance.Challenge.CPUCount ?? 1,
                MemoryLimit = instance.Challenge.MemoryLimit ?? 64,
                StorageLimit = instance.Challenge.StorageLimit ?? 256,
                PrivilegedContainer = instance.Challenge.PrivilegedContainer ?? false,
                ExposedPort = instance.Challenge.ContainerExposePort ?? throw new ArgumentException("创建容器时遇到无效的端口"),
            }, token);

            if (container is null)
            {
                _logger.SystemLog($"为题目 {instance.Challenge.Title} 启动容器实例失败", TaskStatus.Failed, LogLevel.Warning);
                return new TaskResult<Container>(TaskStatus.Failed);
            }

            instance.Container = container;
            instance.LastContainerOperation = DateTimeOffset.UtcNow;

            _logger.Log($"{team.Name} 启动题目 {instance.Challenge.Title} 的容器实例 [{container.ContainerId}]", user, TaskStatus.Success);

            // will save instance together
            await _gameEventRepository.AddEvent(new()
            {
                Type = EventType.ContainerStart,
                GameId = instance.Challenge.GameId,
                TeamId = instance.Participation.TeamId,
                UserId = user.Id,
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
        => _context.Instances.Where(i => i.Challenge == challenge).OrderBy(i => i.ParticipationId)
            .Include(i => i.Participation).ThenInclude(i => i.Team).ToArrayAsync(token);

    public async Task<CheatCheckInfo> CheckCheat(Submission submission, CancellationToken token = default)
    {
        CheatCheckInfo checkInfo = new();

        var instances = await _context.Instances.Where(i => i.ChallengeId == submission.ChallengeId &&
                i.ParticipationId != submission.ParticipationId)
                .Include(i => i.FlagContext).Include(i => i.Participation)
                .ThenInclude(i => i.Team).ToArrayAsync(token);

        foreach (var instance in instances)
        {
            if (instance.FlagContext?.Flag == submission.Answer)
            {
                var updateSub = await _context.Submissions.Where(s => s.Id == submission.Id).SingleAsync(token);

                var cheatInfo = await _cheatInfoRepository.CreateCheatInfo(updateSub, instance, token);
                checkInfo = CheatCheckInfo.FromCheatInfo(cheatInfo);

                if (updateSub is not null)
                    updateSub.Status = AnswerResult.CheatDetected;

                await SaveAsync(token);

                return checkInfo;
            }
        }

        return checkInfo;
    }

    public async Task<VerifyResult> VerifyAnswer(Submission submission, CancellationToken token = default)
    {
        var trans = await _context.Database.BeginTransactionAsync(token);

        try
        {

            var instance = await _context.Instances
                .IgnoreAutoIncludes()
                .Include(i => i.FlagContext)
                .SingleOrDefaultAsync(i => i.ChallengeId == submission.ChallengeId &&
                    i.ParticipationId == submission.ParticipationId, token);

            // submission is from the queue, do not modify it directly
            // we need to requery the entity to ensure it is being tracked correctly
            var updateSub = await _context.Submissions.SingleAsync(s => s.Id == submission.Id, token);

            var ret = SubmissionType.Unaccepted;

            if (instance is null)
            {
                submission.Status = AnswerResult.NotFound;
                return new(SubmissionType.Unaccepted, AnswerResult.NotFound);
            }

            if (instance.FlagContext is null && submission.Challenge.Type.IsStatic())
            {
                updateSub.Status = await _context.FlagContexts
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
            bool beforeEnd = submission.Game.EndTimeUTC > submission.SubmitTimeUTC;

            if (firstTime && beforeEnd)
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
