using CTFServer.Models;
using Docker.DotNet;
using Docker.DotNet.Models;
using CTFServer.Services.Interface;
using CTFServer.Utils;
using NLog;
using CTFServer.Models.Internal;

namespace CTFServer.Services;

public class DockerService : IContainerService
{
    private static readonly Logger logger = LogManager.GetLogger("ContainerService");
    private readonly IConfigurationSection? config;
    private readonly DockerClient dockerClient;
    public DockerService(IConfiguration _configuration)
    {
        config = _configuration.GetSection("DockerConfig") ?? throw new ArgumentException("Docker config required!");

        var uri = config?.GetValue<string>("Uri");

        DockerClientConfiguration cfg = string.IsNullOrEmpty(uri) ? new() : new(new Uri(uri));

        // TODO: Docker Auth Required
        // TODO: Docker Swarm Support

        dockerClient = cfg.CreateClient();
    }

    public Task<Container?> CreateContainer(ContainerConfig config, CancellationToken token = default)
    {
        var parameters = new CreateContainerParameters()
        {
            Image = config.Image,
            Name = $"{config.Image.Split("/").LastOrDefault()}_{Codec.StrMD5(config.Flag ?? Guid.NewGuid().ToString())[..16]}",
            Env = config.Flag is null ? new() : new List<string>{ $"GZCTF_FLAG={config.Flag}" },
            // TODO: Add Health Check
            ExposedPorts = { { config.Port.ToString(), default } },
            HostConfig = new() { 
                PublishAllPorts = true,
                Memory = config.MemoryLimit * 1024 * 1024,
                CPUCount = 1
            }
        };

        return CreateContainer(parameters, token);
    }

    public async Task<Container?> CreateContainer(CreateContainerParameters parameters, CancellationToken token = default)
    {

        // TODO: Docker Registry Auth Required
        var res = await dockerClient.Containers.CreateContainerAsync(parameters, token);

        Container container = new()
        {
            ContainerId = res.ID,
            Image = parameters.Image,
        };

        var retry = 0;
        bool started;
        do
        {
            started = await dockerClient.Containers.StartContainerAsync(res.ID, new(), token);
            retry++;
            if (retry == 3)
            {
                LogHelper.SystemLog(logger, $"启动容器实例 {container.Id[..12]} ({parameters.Image.Split("/").LastOrDefault()}) 失败", TaskStatus.Fail, NLog.LogLevel.Warn);
                return null;
            }
            if (!started)
                await Task.Delay(500, token);
        } while (!started);

        var info = await dockerClient.Containers.InspectContainerAsync(container.ContainerId, token);

        container.Status = (info.State.Dead || info.State.OOMKilled || info.State.Restarting) ? ContainerStatus.Destoryed :
                info.State.Running ? ContainerStatus.Running : ContainerStatus.Pending;

        if (container.Status != ContainerStatus.Running)
        {
            LogHelper.SystemLog(logger, $"创建 {parameters.Image.Split("/").LastOrDefault()} 实例遇到错误：{info.State.Error}", TaskStatus.Fail, NLog.LogLevel.Warn);
            return null;
        }

        container.StartedAt = DateTimeOffset.Parse(info.State.StartedAt);
        container.ExpectStopAt = container.StartedAt + TimeSpan.FromHours(1);
        container.Port = int.Parse(info.NetworkSettings.Ports[parameters.ExposedPorts.First().Key].First().HostPort);

        return container;
    }

    public async Task DestoryContainer(Container container, CancellationToken token = default)
    {
        await dockerClient.Containers.RemoveContainerAsync(container.ContainerId, new() { Force = true }, token);

        container.Status = ContainerStatus.Destoryed;
    }

    public Task<IList<ContainerListResponse>> GetContainers(CancellationToken token = default)
        => dockerClient.Containers.ListContainersAsync(new() { All = true }, token);

    public async Task<(VersionResponse, SystemInfoResponse)> GetHostInfo(CancellationToken token = default)
    {
        var info = await dockerClient.System.GetSystemInfoAsync(token);
        var ver = await dockerClient.System.GetVersionAsync(token);
        return (ver, info);
    }
}
