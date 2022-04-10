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

    public async Task<Participation> CreateInstances(Participation team, CancellationToken token = default)
    {
        // TODO: 生成题目实例的时机策略？
        
        await context.Entry(team).Reference(e => e.Game).LoadAsync(token);
        await context.Entry(team).Reference(e => e.Instances).LoadAsync(token);
        await context.Entry(team.Game).Collection(e => e.Challenges).LoadAsync(token);

        if (team.Instances.Count > 0)
            logger.SystemLog("当前队伍实例列表不为空，即将添加更多实例，这可能造成非预期的行为！", TaskStatus.Pending, LogLevel.Warning);

        foreach (var challenge in team.Game.Challenges)
        {
            if (!challenge.IsEnabled)
                continue;
            
            Instance instance = new()
            {
                Challenge = challenge,
                Game = team.Game,
                Participation = team
            };

            if (challenge.Type.IsStatic())
                instance.Context = null; // use challenge to verify
            else
            {
                if (challenge.Type.IsAttachment())
                {
                    var flags = await context.Entry(challenge).Collection(e => e.Flags).Query().Where(e => !e.IsOccupied).ToListAsync(token);
                    var pos = Random.Shared.Next(flags.Count);
                    flags[pos].IsOccupied = true;
                    instance.Context = flags[pos];
                }
                else
                {
                    instance.Context = new()
                    {
                        AttachmentType = FileType.None,
                        Flag = $"flag{Guid.NewGuid():B}",
                    };
                }
            }

            team.Instances.Add(instance);
            team.Game.Instances.Add(instance);
        }

        await context.SaveChangesAsync(token);

        return team;
    }

    public async Task DestoryContainer(Container container, CancellationToken token = default)
    {
        try
        {
            await service.DestoryContainer(container, token);
            await containerRepository.RemoveContainer(container, token);
            logger.SystemLog($"销毁容器 {container.ContainerId[..12]} ({container.Image.Split("/").LastOrDefault()})", TaskStatus.Success);
        }
        catch (Exception ex)
        {
            logger.SystemLog($"销毁容器 {container.ContainerId[..12]} ({container.Image.Split("/").LastOrDefault()}): {ex.Message}", TaskStatus.Fail, LogLevel.Warning);
        }
    }

    public async Task<TaskResult<Container>> CreateContainer(Instance instance, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(instance.Challenge.ContainerImage) || instance.Challenge.ContainerExposePort is null)
        {
            logger.SystemLog($"无法为题目 {instance.Challenge.Title} 启动容器实例", TaskStatus.Denied, LogLevel.Warning);
            return new TaskResult<Container>(TaskStatus.Denied);
        }

        if (instance.Container is null)
        {
            await context.Entry(instance).Reference(e => e.Context).LoadAsync(token);
            var container = await service.CreateContainer(new ContainerConfig()
            {
                CPUCount = instance.Challenge.CPUCount ?? 1,
                Flag = instance.Context?.Flag ?? throw new ArgumentException("创建容器时遇到无效的 Flag"),
                Image = instance.Challenge.ContainerImage,
                MemoryLimit = instance.Challenge.MemoryLimit ?? 64,
                Port = instance.Challenge.ContainerExposePort?.ToString() ?? throw new ArgumentException("创建容器时遇到无效的端口"),
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

    public Task<Instance[]> GetInstances(Game game, int count = 30, int skip = 0, CancellationToken token = default)
        => context.Instances.Where(i => i.GameId == game.Id).OrderBy(i => i.ParticipationId)
            .Skip(skip).Take(count).Include(i => i.Participation).ThenInclude(i => i.Team)
            .Include(i => i.Challenge).ToArrayAsync(token);

    public async Task<CheatCheckInfo> CheckCheat(Submission submission, CancellationToken token = default)
    {
        CheatCheckInfo checkInfo = new();

        var instances = await context.Instances.Where(i => i.ChallengeId == submission.ChallengeId &&
                i.ParticipationId != submission.ParticipationId)
                .Include(i => i.Challenge).Include(i => i.Context)
                .Include(i => i.Participation).ThenInclude(i => i.Team).ToArrayAsync(token);

        foreach (var instance in instances)
        {
            if(instance.Context?.Flag == submission.Answer)
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
            || (instance.Challenge.Type.IsDynamic() && instance.Context?.Flag == submission.Answer))
            return AnswerResult.Accepted;

        return AnswerResult.WrongAnswer;
    }
}
