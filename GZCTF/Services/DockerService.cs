using CTFServer.Models.Internal;
using CTFServer.Services.Interface;
using CTFServer.Utils;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;
using System.Text;

namespace CTFServer.Services;

public class DockerService : IContainerService
{
    private readonly ILogger<DockerService> logger;
    private readonly DockerConfig options;
    private readonly DockerClient dockerClient;

    public DockerService(IOptions<DockerConfig> _options, IOptions<RegistryConfig> _registry, ILogger<DockerService> _logger)
    {
        options = _options.Value;
        logger = _logger;
        DockerClientConfiguration cfg = string.IsNullOrEmpty(this.options.Uri) ? new() : new(new Uri(this.options.Uri));

        // TODO: Docker Auth Required
        // TODO: Docker Swarm Support

        dockerClient = cfg.CreateClient();

        // Auth for registry
        if (!string.IsNullOrWhiteSpace(_registry.Value.ServerAddress)
            && !string.IsNullOrWhiteSpace(_registry.Value.UserName)
            && !string.IsNullOrWhiteSpace(_registry.Value.Password))
        {
            dockerClient.System.AuthenticateAsync(new() {
                Username = _registry.Value.UserName,
                Password = _registry.Value.Password,
                ServerAddress = _registry.Value.ServerAddress,
            }).Wait();
        }

        if (string.IsNullOrEmpty(this.options.Uri))
            logger.SystemLog($"Docker 服务已启动 (localhost)", TaskStatus.Success, LogLevel.Debug);
        else
            logger.SystemLog($"Docker 服务已启动 ({this.options.Uri})", TaskStatus.Success, LogLevel.Debug);
    }

    public Task<Container?> CreateContainer(ContainerConfig config, CancellationToken token = default)
    {
        // TODO: Add Health Check
        var parameters = new CreateContainerParameters()
        {
            Image = config.Image,
            Labels = new Dictionary<string, string> { { "TeamInfo", config.TeamInfo } },
            Name = $"{config.Image.Split("/").LastOrDefault()?.Split(":").FirstOrDefault()}_{Codec.StrMD5(config.Flag ?? Guid.NewGuid().ToString())[..16]}",
            Env = config.Flag is null ? new() : new List<string> { $"GZCTF_FLAG={config.Flag}" },
            ExposedPorts = new Dictionary<string, EmptyStruct>() {
                { config.ExposedPort.ToString(), new EmptyStruct() }
            },
            HostConfig = new()
            {
                PublishAllPorts = true,
                Memory = config.MemoryLimit * 1024 * 1024,
                CPUCount = 1
            }
        };

        return CreateContainerByParams(parameters, token);
    }

    private async Task<Container?> CreateContainerByParams(CreateContainerParameters parameters, CancellationToken token = default)
    {
        CreateContainerResponse? res = null;
        try
        {
            res = await dockerClient.Containers.CreateContainerAsync(parameters, token);
        }
        catch (DockerImageNotFoundException)
        {
            logger.SystemLog($"拉取容器镜像 {parameters.Image}", TaskStatus.Pending, LogLevel.Information);

            await dockerClient.Images.CreateImageAsync(new()
            {
                FromImage = parameters.Image
            }, null, new Progress<JSONMessage>(msg =>
            {
                Console.WriteLine($"{msg.Status}|{msg.ProgressMessage}|{msg.ErrorMessage}");
            }), token);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message, e);
            return null;
        }

        if (res is null)
        {
            try
            {
                res = await dockerClient.Containers.CreateContainerAsync(parameters, token);
            }
            catch (Exception e)
            {
                logger.LogError(e.Message, e);
                return null;
            }
        }

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
                logger.SystemLog($"启动容器实例 {container.Id[..12]} ({parameters.Image.Split("/").LastOrDefault()}) 失败", TaskStatus.Fail, LogLevel.Warning);
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
            logger.SystemLog($"创建 {parameters.Image.Split("/").LastOrDefault()} 实例遇到错误：{info.State.Error}", TaskStatus.Fail, LogLevel.Warning);
            return null;
        }

        container.StartedAt = DateTimeOffset.Parse(info.State.StartedAt);
        container.ExpectStopAt = container.StartedAt + TimeSpan.FromHours(2);
        container.Port = int.Parse(info.NetworkSettings.Ports
            .FirstOrDefault(p =>
                p.Key.StartsWith(parameters.ExposedPorts.First().Key)
            ).Value.First().HostPort);

        if (!string.IsNullOrEmpty(options.PublicIP))
            container.PublicIP = options.PublicIP;

        return container;
    }

    public async Task DestoryContainer(Container container, CancellationToken token = default)
    {
        try
        {
            await dockerClient.Containers.RemoveContainerAsync(container.ContainerId, new() { Force = true }, token);
        }
        catch (DockerContainerNotFoundException)
        {
            logger.SystemLog($"容器 {container.ContainerId[..12]} 已被销毁", TaskStatus.Success, LogLevel.Debug);
        }
        catch (Exception e)
        {
            logger.LogError(e, "删除容器失败");
            return;
        }

        container.Status = ContainerStatus.Destoryed;
    }

    public async Task<string> GetHostInfo(CancellationToken token = default)
    {
        var info = await dockerClient.System.GetSystemInfoAsync(token);
        var ver = await dockerClient.System.GetVersionAsync(token);

        StringBuilder output = new();

        output.AppendLine("[[ Version ]]");
        foreach (var item in ver.GetType().GetProperties())
        {
            var val = item.GetValue(ver);
            if (val is IEnumerable<object> vals)
                val = string.Join(", ", vals);
            output.AppendLine($"{item.Name,-20}: {val}");
        }

        output.AppendLine("[[ Info ]]");
        foreach (var item in info.GetType().GetProperties())
        {
            var val = item.GetValue(info);
            if (val is IEnumerable<object> vals)
                val = string.Join(", ", vals);
            output.AppendLine($"{item.Name,-20}: {val}");
        }

        return output.ToString();
    }

    public async Task<IList<ContainerInfo>> GetContainers(CancellationToken token)
    {
        var containers = await dockerClient.Containers.ListContainersAsync(new() { All = true }, token);
        return (from c in containers
                select new ContainerInfo()
                {
                    Id = c.ID,
                    Image = c.Image,
                    State = c.State,
                    Name = c.Names[0]
                }).ToArray();
    }

    public async Task<Container> QueryContainer(Container container, CancellationToken token = default)
    {
        var info = await dockerClient.Containers.InspectContainerAsync(container.ContainerId, token);

        container.Status = (info.State.Dead || info.State.OOMKilled || info.State.Restarting) ? ContainerStatus.Destoryed :
                info.State.Running ? ContainerStatus.Running : ContainerStatus.Pending;

        return container;
    }
}