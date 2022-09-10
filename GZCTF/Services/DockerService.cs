using CTFServer.Models.Internal;
using CTFServer.Services.Interface;
using CTFServer.Utils;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CTFServer.Services;

public class DockerService : IContainerService
{
    private readonly ILogger<DockerService> logger;
    private readonly DockerConfig options;
    private readonly DockerClient dockerClient;
    private readonly AuthConfig? authConfig;

    public DockerService(IOptions<DockerConfig> _options, IOptions<RegistryConfig> _registry, ILogger<DockerService> _logger)
    {
        options = _options.Value;
        logger = _logger;
        DockerClientConfiguration cfg = string.IsNullOrEmpty(this.options.Uri) ? new() : new(new Uri(this.options.Uri));

        // TODO: Docker Auth Required
        // TODO: Docker Swarm Support

        dockerClient = cfg.CreateClient();

        // Auth for registry
        if (!string.IsNullOrWhiteSpace(_registry.Value.UserName) && !string.IsNullOrWhiteSpace(_registry.Value.Password))
        {
            authConfig = new AuthConfig()
            {
                Username = _registry.Value.UserName,
                Password = _registry.Value.Password,
            };
        }

        logger.SystemLog($"Docker 服务已启动 ({(string.IsNullOrEmpty(this.options.Uri) ? "localhost" : this.options.Uri)})", TaskStatus.Success, LogLevel.Debug);
    }

    public Task<Container?> CreateContainer(ContainerConfig config, CancellationToken token = default)
        => options.SwarmMode ? CreateContainerWithSwarm(config, token) : CreateContainerWithSingle(config, token);

    public async Task DestoryContainer(Container container, CancellationToken token = default)
    {
        try
        {
            if (options.SwarmMode)
                await dockerClient.Swarm.RemoveServiceAsync(container.ContainerId, token);
            else
                await dockerClient.Containers.RemoveContainerAsync(container.ContainerId, new() { Force = true }, token);
        }
        catch (DockerContainerNotFoundException)
        {
            logger.SystemLog($"容器 {container.ContainerId[..12]} 已被销毁", TaskStatus.Success, LogLevel.Debug);
        }
        catch (DockerApiException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                container.Status = ContainerStatus.Destoryed;
                return;
            }
            logger.SystemLog($"容器 {container.ContainerId} 删除失败, 状态：{e.StatusCode.ToString()}", TaskStatus.Fail, LogLevel.Warning);
            logger.SystemLog($"容器 {container.ContainerId} 删除失败, 响应：{e.ResponseBody}", TaskStatus.Fail, LogLevel.Error);
        }
        catch (Exception e)
        {
            logger.LogError(e, "删除容器失败");
            return;
        }

        container.Status = ContainerStatus.Destoryed;
    }

    private static string GetName(ContainerConfig config)
        => $"{config.Image.Split("/").LastOrDefault()?.Split(":").FirstOrDefault()}_{Codec.StrMD5(config.Flag ?? Guid.NewGuid().ToString())[..16]}";

    private CreateContainerParameters GetCreateContainerParameters(ContainerConfig config)
        => new()
        {
            Image = config.Image,
            Labels = new Dictionary<string, string> { { "TeamId", config.TeamId }, { "UserId", config.UserId } },
            Name = GetName(config),
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

    private ServiceCreateParameters GetServiceCreateParameters(ContainerConfig config)
        => new()
        {
            RegistryAuth = authConfig,
            Service = new()
            {
                Name = GetName(config),
                Labels = new Dictionary<string, string> { { "TeamId", config.TeamId }, { "UserId", config.UserId } },
                Mode = new () { Replicated = new () { Replicas = 1 } },
                EndpointSpec = new()
                {
                    Ports = new PortConfig[1] { new() {
                        PublishMode = "global",
                        TargetPort = (uint)config.ExposedPort, 
                    } },
                },
                TaskTemplate = new()
                {
                    RestartPolicy = new () { Condition = "none" },
                    ContainerSpec = new ()
                    {
                        Image = config.Image,
                        Env = config.Flag is null ? new() : new List<string> { $"GZCTF_FLAG={config.Flag}" }
                    },
                    Resources = new()
                    {
                        Limits = new()
                        {
                            MemoryBytes = config.MemoryLimit * 1024 * 1024,
                            NanoCPUs = config.CPUCount * 10_0000_0000, // do some math here?
                        }
                    },
                }
            }
        };

    public async Task<Container?> CreateContainerWithSwarm(ContainerConfig config, CancellationToken token = default)
    {
        var parameters = GetServiceCreateParameters(config);
        ServiceCreateResponse? serviceRes = null;
        try
        {
            serviceRes = await dockerClient.Swarm.CreateServiceAsync(parameters, token);
        }
        catch (DockerApiException e)
        {
            logger.SystemLog($"容器 {parameters.Service.Name} 创建失败, 状态：{e.StatusCode.ToString()}", TaskStatus.Fail, LogLevel.Warning);
            logger.SystemLog($"容器 {parameters.Service.Name} 创建失败, 响应：{e.ResponseBody}", TaskStatus.Fail, LogLevel.Error);
            return null;
        }
        catch (Exception e)
        {
            logger.LogError(e.Message, e);
            return null;
        }

        Container container = new()
        {
            ContainerId = serviceRes.ID,
            Image = config.Image,
        };

        var res = await dockerClient.Swarm.InspectServiceAsync(serviceRes.ID, token);

        var port = res.Endpoint.Ports.FirstOrDefault();

        if (port is null)
        {
            logger.SystemLog($"未获取到容器暴露端口信息，这可能是意料外的行为", TaskStatus.Fail, LogLevel.Warning);
            return null;
        }

        container.StartedAt = res.CreatedAt;
        container.ExpectStopAt = container.StartedAt + TimeSpan.FromHours(2);

        container.Port = (int)port.PublishedPort;
        container.Status = ContainerStatus.Running;

        // FIXME: how to get IP?
        if (!string.IsNullOrEmpty(options.PublicIP))
            container.PublicIP = options.PublicIP;

        return container;
    }

    public async Task<Container?> CreateContainerWithSingle(ContainerConfig config, CancellationToken token = default)
    {
        var parameters = GetCreateContainerParameters(config);
        CreateContainerResponse? containerRes = null;
        try
        {
           containerRes = await dockerClient.Containers.CreateContainerAsync(parameters, token);
        }
        catch (DockerImageNotFoundException)
        {
            logger.SystemLog($"拉取容器镜像 {config.Image}", TaskStatus.Pending, LogLevel.Information);

            await dockerClient.Images.CreateImageAsync(new()
            {
                FromImage = config.Image
            }, authConfig, new Progress<JSONMessage>(msg =>
            {
                Console.WriteLine($"{msg.Status}|{msg.ProgressMessage}|{msg.ErrorMessage}");
            }), token);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message, e);
            return null;
        }

        if (containerRes is null)
        {
            try
            {
                containerRes = await dockerClient.Containers.CreateContainerAsync(parameters, token);
            }
            catch (Exception e)
            {
                logger.LogError(e.Message, e);
                return null;
            }
        }

        Container container = new()
        {
            ContainerId = containerRes.ID,
            Image = config.Image,
        };

        var retry = 0;
        bool started;

        do
        {
            started = await dockerClient.Containers.StartContainerAsync(containerRes.ID, new(), token);
            retry++;
            if (retry == 3)
            {
                logger.SystemLog($"启动容器实例 {container.Id[..12]} ({config.Image.Split("/").LastOrDefault()}) 失败", TaskStatus.Fail, LogLevel.Warning);
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
            logger.SystemLog($"创建 {config.Image.Split("/").LastOrDefault()} 实例遇到错误：{info.State.Error}", TaskStatus.Fail, LogLevel.Warning);
            return null;
        }

        container.StartedAt = DateTimeOffset.Parse(info.State.StartedAt);
        container.ExpectStopAt = container.StartedAt + TimeSpan.FromHours(2);
        container.Port = int.Parse(info.NetworkSettings.Ports
            .FirstOrDefault(p =>
                p.Key.StartsWith(config.ExposedPort.ToString())
            ).Value.First().HostPort);

        container.IP = info.NetworkSettings.IPAddress;

        if (!string.IsNullOrEmpty(options.PublicIP))
            container.PublicIP = options.PublicIP;

        return container;
    }
}