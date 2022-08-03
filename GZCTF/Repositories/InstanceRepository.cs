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
    private readonly ILogger<InstanceRepository> logger;

    public InstanceRepository(AppDbContext _context,
        IContainerRepository _containerRepository,
        IContainerService _service,
        ILogger<InstanceRepository> _logger) : base(_context)
    {
        service = _service;
        containerRepository = _containerRepository;
        logger = _logger;
    }

    public async Task<Instance?> GetInstance(Participation team, int challengeId, CancellationToken token = default)
    {
        var instance = await context.Instances
            .Where(e => e.ChallengeId == challengeId && e.Participation == team)
            .SingleOrDefaultAsync(token);

        if (instance is null)
        {
            logger.SystemLog($"队伍对应参与对象为空，这可能是非预期的情况 [{team.Id}, {challengeId}]", TaskStatus.NotFound, LogLevel.Warning);
            return null;
        }

        if (instance.IsLoaded)
            return instance;

        var challenge = await context.Challenges.FirstOrDefaultAsync(c => c.Id == challengeId, token);

        if (challenge is null || !challenge.IsEnabled)
            return null;

        if (challenge.Type.IsStatic())
            instance.FlagContext = null; // use challenge to verify
        else
        {
            if (challenge.Type.IsAttachment())
            {
                var flags = await context.Entry(challenge).Collection(e => e.Flags)
                        .Query().Where(e => !e.IsOccupied).ToListAsync(token);
                var pos = Random.Shared.Next(flags.Count);
                flags[pos].IsOccupied = true;
                instance.FlagContext = flags[pos];
            }
            else
            {
                instance.FlagContext = new()
                {
                    Flag = $"flag{Guid.NewGuid():B}",
                    IsOccupied = true
                };
            }
        }

        instance.IsLoaded = true;

        await UpdateAsync(instance, token);

        return instance;
    }

    public async Task<bool> DestoryContainer(Container container, CancellationToken token = default)
    {
        try
        {
            await service.DestoryContainer(container, token);
            await containerRepository.RemoveContainer(container, token);
            logger.SystemLog($"销毁容器 {container.ContainerId[..12]} ({container.Image.Split("/").LastOrDefault()})", TaskStatus.Success);
            return true;
        }
        catch (Exception ex)
        {
            logger.SystemLog($"销毁容器 {container.ContainerId[..12]} ({container.Image.Split("/").LastOrDefault()}): {ex.Message}", TaskStatus.Fail, LogLevel.Warning);
            return false;
        }
    }

    public async Task<TaskResult<Container>> CreateContainer(Instance instance, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(instance.Challenge.ContainerImage) || instance.Challenge.ContainerExposePort is null)
        {
            logger.SystemLog($"无法为题目 {instance.Challenge.Title} 启动容器实例", TaskStatus.Denied, LogLevel.Warning);
            return new TaskResult<Container>(TaskStatus.Fail);
        }

        if (context.Instances.Count(i => i.Participation == instance.Participation
            && i.Container != null
            && i.Container.Status == ContainerStatus.Running) == 3)
            return new TaskResult<Container>(TaskStatus.Denied);

        if (instance.Container is null)
        {
            await context.Entry(instance).Reference(e => e.FlagContext).LoadAsync(token);
            var container = await service.CreateContainer(new ContainerConfig()
            {
                CPUCount = instance.Challenge.CPUCount ?? 1,
                Flag = instance.FlagContext?.Flag, // static challenge has no specific flag
                Image = instance.Challenge.ContainerImage,
                MemoryLimit = instance.Challenge.MemoryLimit ?? 64,
                ExposedPort = instance.Challenge.ContainerExposePort ?? throw new ArgumentException("创建容器时遇到无效的端口"),
            }, token);
            instance.Container = container;

            await context.SaveChangesAsync(token);
        }

        return new TaskResult<Container>(TaskStatus.Success, instance.Container);
    }

    public async Task ProlongContainer(Container container, TimeSpan time, CancellationToken token = default)
    {
        container.ExpectStopAt += time;
        await context.SaveChangesAsync(token);
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
                await context.Entry(submission).Reference(s => s.User).LoadAsync(token);
                await context.Entry(submission).Reference(s => s.Participation.Team).LoadAsync(token);

                checkInfo.AnswerResult = AnswerResult.CheatDetected;
                checkInfo.CheatUser = submission.User;
                checkInfo.CheatTeam = submission.Participation.Team;
                checkInfo.SourceTeam = instance.Participation.Team;
                checkInfo.Challenge = instance.Challenge;

                return checkInfo;
            }
        }

        return checkInfo;
    }

    public async Task<AnswerResult> VerifyAnswer(Submission submission, CancellationToken token = default)
    {
        var instance = await context.Instances.SingleOrDefaultAsync(i => i.ChallengeId == submission.ChallengeId && i.ParticipationId == submission.ParticipationId, token);

        if (instance is null)
            return AnswerResult.NotFound;

        if ((instance.Challenge.Type.IsStatic() && instance.Challenge.Flags.Any(f => f.Flag == submission.Answer))
            || (instance.Challenge.Type.IsDynamic() && instance.FlagContext?.Flag == submission.Answer))
            return AnswerResult.Accepted;

        return AnswerResult.WrongAnswer;
    }

    public async Task<bool> TrySolved(Submission submission, CancellationToken token = default)
    {
        var instance = await context.Instances.SingleOrDefaultAsync(i => i.ChallengeId == submission.ChallengeId && i.ParticipationId == submission.ParticipationId, token);

        if (instance is null)
            throw new NullReferenceException(nameof(instance));

        if (instance.IsSolved)
            return false;

        instance.IsSolved = true;
        await context.SaveChangesAsync(token);
        return true;
    }
}