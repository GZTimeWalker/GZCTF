using System.Net;
using Docker.DotNet;
using Docker.DotNet.Models;
using GZCTF.Models.Internal;
using GZCTF.Services.Interface;
using GZCTF.Utils;
using Microsoft.Extensions.Options;

namespace GZCTF.Services;

public class DockerService : IContainerService
{
    private readonly ILogger<DockerService> _logger;
    private readonly DockerConfig _options;
    private readonly string _publicEntry;
    private readonly DockerClient _dockerClient;
    private readonly AuthConfig? _authConfig;

    public DockerService(IOptions<ContainerProvider> options, IOptions<RegistryConfig> registry, ILogger<DockerService> logger)
    {
        _options = options.Value.DockerConfig ?? new();
        _publicEntry = options.Value.PublicEntry;
        _logger = logger;
        DockerClientConfiguration cfg = string.IsNullOrEmpty(_options.Uri) ? new() : new(new Uri(_options.Uri));

        // TODO: Docker Auth Required
        _dockerClient = cfg.CreateClient();

        // Auth for registry
        if (!string.IsNullOrWhiteSpace(registry.Value.UserName) && !string.IsNullOrWhiteSpace(registry.Value.Password))
        {
            _authConfig = new AuthConfig()
            {
                Username = registry.Value.UserName,
                Password = registry.Value.Password,
            };
        }

        logger.SystemLog($"Docker 服务已启动 ({(string.IsNullOrEmpty(_options.Uri) ? "localhost" : _options.Uri)})", TaskStatus.Success, LogLevel.Debug);
    }

    public Task<Container?> CreateContainerAsync(ContainerConfig config, CancellationToken token = default)
        => _options.SwarmMode ? CreateContainerWithSwarm(config, token) : CreateContainerWithSingle(config, token);

    public async Task DestroyContainerAsync(Container container, CancellationToken token = default)
    {
        try
        {
            if (_options.SwarmMode)
                await _dockerClient.Swarm.RemoveServiceAsync(container.ContainerId, token);
            else
                await _dockerClient.Containers.RemoveContainerAsync(container.ContainerId, new() { Force = true }, token);
        }
        catch (DockerContainerNotFoundException)
        {
            _logger.SystemLog($"容器 {container.ContainerId} 已被销毁", TaskStatus.Success, LogLevel.Debug);
        }
        catch (DockerApiException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.SystemLog($"容器 {container.ContainerId} 已被销毁", TaskStatus.Success, LogLevel.Debug);
            }
            else
            {
                _logger.SystemLog($"容器 {container.ContainerId} 删除失败, 状态：{e.StatusCode}", TaskStatus.Failed, LogLevel.Warning);
                _logger.SystemLog($"容器 {container.ContainerId} 删除失败, 响应：{e.ResponseBody}", TaskStatus.Failed, LogLevel.Error);
                return;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"容器 {container.ContainerId} 删除失败");
            return;
        }

        container.Status = ContainerStatus.Destroyed;
    }

    private static string GetName(ContainerConfig config)
        => $"{config.Image.Split("/").LastOrDefault()?.Split(":").FirstOrDefault()}_{(config.Flag ?? Guid.NewGuid().ToString()).StrMD5()[..16]}";

    private static CreateContainerParameters GetCreateContainerParameters(ContainerConfig config)
        => new()
        {
            Image = config.Image,
            Labels = new Dictionary<string, string> { ["TeamId"] = config.TeamId, ["UserId"] = config.UserId },
            Name = GetName(config),
            Env = config.Flag is null ? Array.Empty<string>() : new string[] { $"GZCTF_FLAG={config.Flag}" },
            ExposedPorts = new Dictionary<string, EmptyStruct>()
            {
                [config.ExposedPort.ToString()] = new EmptyStruct()
            },
            HostConfig = new()
            {
                PublishAllPorts = true,
                Memory = config.MemoryLimit * 1024 * 1024,
                CPUPercent = config.CPUCount * 10,
                Privileged = config.PrivilegedContainer
            }
        };

    private ServiceCreateParameters GetServiceCreateParameters(ContainerConfig config)
        => new()
        {
            RegistryAuth = _authConfig,
            Service = new()
            {
                Name = GetName(config),
                Labels = new Dictionary<string, string> { ["TeamId"] = config.TeamId, ["UserId"] = config.UserId },
                Mode = new() { Replicated = new() { Replicas = 1 } },
                EndpointSpec = new()
                {
                    Ports = new PortConfig[] { new() {
                        PublishMode = "global",
                        TargetPort = (uint)config.ExposedPort,
                    } },
                },
                TaskTemplate = new()
                {
                    RestartPolicy = new() { Condition = "none" },
                    ContainerSpec = new()
                    {
                        Image = config.Image,
                        Env = config.Flag is null ? Array.Empty<string>() : new string[] { $"GZCTF_FLAG={config.Flag}" }
                    },
                    Resources = new()
                    {
                        Limits = new()
                        {
                            MemoryBytes = config.MemoryLimit * 1024 * 1024,
                            NanoCPUs = config.CPUCount * 1_0000_0000,
                        },
                    },
                }
            }
        };

    private async Task<Container?> CreateContainerWithSwarm(ContainerConfig config, CancellationToken token = default)
    {
        var parameters = GetServiceCreateParameters(config);
        int retry = 0;
        ServiceCreateResponse? serviceRes;
    CreateContainer:
        try
        {
            serviceRes = await _dockerClient.Swarm.CreateServiceAsync(parameters, token);
        }
        catch (DockerApiException e)
        {
            if (e.StatusCode == HttpStatusCode.Conflict && retry < 3)
            {
                _logger.SystemLog($"容器 {parameters.Service.Name} 已存在，尝试移除后重新创建", TaskStatus.Duplicate, LogLevel.Warning);
                await _dockerClient.Swarm.RemoveServiceAsync(parameters.Service.Name, token);
                retry++;
                goto CreateContainer;
            }
            else
            {
                _logger.SystemLog($"容器 {parameters.Service.Name} 创建失败, 状态：{e.StatusCode}", TaskStatus.Failed, LogLevel.Warning);
                _logger.SystemLog($"容器 {parameters.Service.Name} 创建失败, 响应：{e.ResponseBody}", TaskStatus.Failed, LogLevel.Error);
                return null;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"容器 {parameters.Service.Name} 删除失败");
            return null;
        }

        Container container = new()
        {
            ContainerId = serviceRes.ID,
            Image = config.Image,
        };

        retry = 0;
        SwarmService? res;
        do
        {
            res = await _dockerClient.Swarm.InspectServiceAsync(serviceRes.ID, token);
            retry++;
            if (retry == 3)
            {
                _logger.SystemLog($"容器 {parameters.Service.Name} 创建后未获取到端口暴露信息，创建失败", TaskStatus.Failed, LogLevel.Warning);
                return null;
            }
            if (res is not { Endpoint.Ports.Count: > 0 })
                await Task.Delay(500, token);
        } while (res is not { Endpoint.Ports.Count: > 0 });

        var port = res.Endpoint.Ports.First();

        container.StartedAt = res.CreatedAt;
        container.ExpectStopAt = container.StartedAt + TimeSpan.FromHours(2);

        container.Port = (int)port.PublishedPort;
        container.Status = ContainerStatus.Running;

        if (!string.IsNullOrEmpty(_publicEntry))
            container.PublicIP = _publicEntry;

        return container;
    }

    private async Task<Container?> CreateContainerWithSingle(ContainerConfig config, CancellationToken token = default)
    {
        var parameters = GetCreateContainerParameters(config);
        CreateContainerResponse? containerRes = null;
        try
        {
            containerRes = await _dockerClient.Containers.CreateContainerAsync(parameters, token);
        }
        catch (DockerImageNotFoundException)
        {
            _logger.SystemLog($"拉取容器镜像 {config.Image}", TaskStatus.Pending, LogLevel.Information);

            await _dockerClient.Images.CreateImageAsync(new()
            {
                FromImage = config.Image
            }, _authConfig, new Progress<JSONMessage>(msg =>
            {
                Console.WriteLine($"{msg.Status}|{msg.ProgressMessage}|{msg.ErrorMessage}");
            }), token);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"容器 {parameters.Name} 创建失败");
            return null;
        }

        try
        {
            containerRes ??= await _dockerClient.Containers.CreateContainerAsync(parameters, token);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"容器 {parameters.Name} 创建失败");
            return null;
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
            started = await _dockerClient.Containers.StartContainerAsync(containerRes.ID, new(), token);
            retry++;
            if (retry == 3)
            {
                _logger.SystemLog($"启动容器实例 {container.ContainerId[..12]} ({config.Image.Split("/").LastOrDefault()}) 失败", TaskStatus.Failed, LogLevel.Warning);
                return null;
            }
            if (!started)
                await Task.Delay(500, token);
        } while (!started);

        var info = await _dockerClient.Containers.InspectContainerAsync(container.ContainerId, token);

        container.Status = (info.State.Dead || info.State.OOMKilled || info.State.Restarting) ? ContainerStatus.Destroyed :
                info.State.Running ? ContainerStatus.Running : ContainerStatus.Pending;

        if (container.Status != ContainerStatus.Running)
        {
            _logger.SystemLog($"创建 {config.Image.Split("/").LastOrDefault()} 实例遇到错误：{info.State.Error}", TaskStatus.Failed, LogLevel.Warning);
            return null;
        }

        container.StartedAt = DateTimeOffset.Parse(info.State.StartedAt);
        container.ExpectStopAt = container.StartedAt + TimeSpan.FromHours(2);

        var port = info.NetworkSettings.Ports
            .FirstOrDefault(p =>
                p.Key.StartsWith(config.ExposedPort.ToString())
            ).Value.First().HostPort;

        if (int.TryParse(port, out var numport))
            container.Port = numport;
        else
            _logger.SystemLog($"无法转换端口号：{port}，这是非预期的行为", TaskStatus.Failed, LogLevel.Warning);

        container.IP = info.NetworkSettings.IPAddress;

        if (!string.IsNullOrEmpty(_publicEntry))
            container.PublicIP = _publicEntry;

        return container;
    }
}
