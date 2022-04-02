using CTFServer.Models;
using CTFServer.Models.Internal;
using CTFServer.Repositories.Interface;
using CTFServer.Services.Interface;
using CTFServer.Utils;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace CTFServer.Repositories;

public class InstanceRepository : RepositoryBase, IInstanceRepository
{
    private readonly IContainerService service;
    private readonly IContainerRepository containerRepository;
    private static readonly Logger logger = LogManager.GetLogger("InstanceRepository");

    public InstanceRepository(AppDbContext _context,
        IContainerRepository _containerRepository,
        IContainerService _service) : base(_context) 
    {
        service = _service;
        containerRepository = _containerRepository;
    }

    public async Task<Participation> CreateInstances(Participation team, CancellationToken token = default)
    {
        await context.Entry(team).Reference(e => e.Game).LoadAsync(token);
        await context.Entry(team).Reference(e => e.Instances).LoadAsync(token);
        await context.Entry(team.Game).Collection(e => e.Challenges).LoadAsync(token);

        if (team.Instances.Count > 0)
            LogHelper.SystemLog(logger, "当前队伍实例列表不为空，即将添加更多实例，这可能造成非预期的行为！", TaskStatus.Pending, NLog.LogLevel.Warn);

        foreach(var challenge in team.Game.Challenges)
        {
            Instance instance = new()
            {
                Challenge = challenge,
                Game = team.Game,
                Team = team
            };

            if(challenge.Type.IsStatic())
                instance.Flag = null; // use challenge to verify
            else
            {
                if (challenge.Type.IsAttachment())
                {
                    var flags = await context.Entry(challenge).Collection(e => e.Flags).Query().Where(e => !e.IsOccupied).ToListAsync(token);
                    var pos = Random.Shared.Next(flags.Count);
                    flags[pos].IsOccupied = true;
                    instance.Flag = flags[pos];
                }
                else
                {
                    instance.Flag = new()
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
            LogHelper.SystemLog(logger, $"销毁容器 {container.ContainerId[..12]} ({container.Image.Split("/").LastOrDefault()})", TaskStatus.Success);
        }
        catch (Exception ex)
        {
            LogHelper.SystemLog(logger, $"销毁容器 {container.ContainerId[..12]} ({container.Image.Split("/").LastOrDefault()}): {ex.Message}", TaskStatus.Fail, NLog.LogLevel.Warn);
        }
    }

    public Task<Instance?> GetInstance(Participation team, Challenge challenge, CancellationToken token = default)
        => context.Instances.SingleOrDefaultAsync(i => i.ChallengeId == challenge.Id && i.TeamId == team.Id, token);

    public async Task<TaskResult<Container>> CreateContainer(Instance instance, CancellationToken token = default)
    {
        if(string.IsNullOrEmpty(instance.Challenge.ContainerImage) || instance.Challenge.ContainerExposePort is null)
        {
            LogHelper.SystemLog(logger, $"无法为题目 {instance.Challenge.Title} 启动容器实例", TaskStatus.Denied, NLog.LogLevel.Warn);
            return new TaskResult<Container>(TaskStatus.Denied);
        }

        if (instance.Container is null)
        {
            await context.Entry(instance).Reference(e => e.Flag).LoadAsync(token);
            var container = await service.CreateContainer(new ContainerConfig()
            {
                CPUCount = instance.Challenge.CPUCount ?? 1,
                Flag = instance.Flag?.Flag ?? throw new ArgumentException("创建容器时遇到无效的 Flag"),
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
}
