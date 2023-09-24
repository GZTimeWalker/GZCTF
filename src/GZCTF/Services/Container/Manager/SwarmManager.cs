using System.Net;
using Docker.DotNet;
using Docker.DotNet.Models;
using GZCTF.Models.Internal;
using GZCTF.Services.Container.Provider;
using GZCTF.Services.Interface;
using ContainerStatus = GZCTF.Utils.ContainerStatus;

namespace GZCTF.Services.Container.Manager;

public class SwarmManager : IContainerManager
{
    readonly DockerClient _client;
    readonly ILogger<SwarmManager> _logger;
    readonly DockerMetadata _meta;

    public SwarmManager(IContainerProvider<DockerClient, DockerMetadata> provider, ILogger<SwarmManager> logger)
    {
        _logger = logger;
        _meta = provider.GetMetadata();
        _client = provider.GetProvider();

        logger.SystemLog("容器管理模式：Docker Swarm 集群容器控制", TaskStatus.Success, LogLevel.Debug);
    }

    public async Task DestroyContainerAsync(Models.Data.Container container, CancellationToken token = default)
    {
        try
        {
            await _client.Swarm.RemoveServiceAsync(container.ContainerId, token);
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
                _logger.SystemLog($"容器 {container.ContainerId} 删除失败, 状态：{e.StatusCode}", TaskStatus.Failed,
                    LogLevel.Warning);
                _logger.SystemLog($"容器 {container.ContainerId} 删除失败, 响应：{e.ResponseBody}", TaskStatus.Failed,
                    LogLevel.Error);
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

    public async Task<Models.Data.Container?> CreateContainerAsync(ContainerConfig config,
        CancellationToken token = default)
    {
        ServiceCreateParameters parameters = GetServiceCreateParameters(config);
        var retry = 0;
        ServiceCreateResponse? serviceRes;
    CreateContainer:
        try
        {
            serviceRes = await _client.Swarm.CreateServiceAsync(parameters, token);
        }
        catch (DockerApiException e)
        {
            if (e.StatusCode == HttpStatusCode.Conflict && retry < 3)
            {
                _logger.SystemLog($"容器 {parameters.Service.Name} 已存在，尝试移除后重新创建", TaskStatus.Duplicate,
                    LogLevel.Warning);
                await _client.Swarm.RemoveServiceAsync(parameters.Service.Name, token);
                retry++;
                goto CreateContainer;
            }

            _logger.SystemLog($"容器 {parameters.Service.Name} 创建失败, 状态：{e.StatusCode}", TaskStatus.Failed,
                LogLevel.Warning);
            _logger.SystemLog($"容器 {parameters.Service.Name} 创建失败, 响应：{e.ResponseBody}", TaskStatus.Failed,
                LogLevel.Error);
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"容器 {parameters.Service.Name} 删除失败");
            return null;
        }

        var container = new Models.Data.Container { ContainerId = serviceRes.ID, Image = config.Image };

        retry = 0;
        SwarmService? res;
        do
        {
            res = await _client.Swarm.InspectServiceAsync(container.ContainerId, token);
            retry++;
            if (retry == 3)
            {
                _logger.SystemLog($"容器 {container.ContainerId} 创建后未获取到端口暴露信息，创建失败", TaskStatus.Failed,
                    LogLevel.Warning);
                return null;
            }

            if (res is not { Endpoint.Ports.Count: > 0 })
                await Task.Delay(500, token);
        } while (res is not { Endpoint.Ports.Count: > 0 });

        // TODO: Test is needed
        container.Status = ContainerStatus.Running;
        container.StartedAt = res.CreatedAt;
        container.ExpectStopAt = container.StartedAt + TimeSpan.FromHours(2);
        container.IP = res.Endpoint.VirtualIPs.First().Addr;
        container.Port = (int)res.Endpoint.Ports.First().PublishedPort;
        container.IsProxy = !_meta.ExposePort;

        if (_meta.ExposePort)
        {
            container.PublicPort = container.Port;
            if (!string.IsNullOrEmpty(_meta.PublicEntry))
                container.PublicIP = _meta.PublicEntry;
        }

        return container;
    }

    ServiceCreateParameters GetServiceCreateParameters(ContainerConfig config) =>
        new()
        {
            RegistryAuth = _meta.Auth,
            Service = new()
            {
                Name = DockerMetadata.GetName(config),
                Labels =
                    new Dictionary<string, string> { ["TeamId"] = config.TeamId, ["UserId"] = config.UserId },
                Mode = new() { Replicated = new() { Replicas = 1 } },
                TaskTemplate = new()
                {
                    RestartPolicy = new() { Condition = "none" },
                    ContainerSpec =
                        new()
                        {
                            Image = config.Image,
                            Env =
                                config.Flag is null
                                    ? Array.Empty<string>()
                                    : new[] { $"GZCTF_FLAG={config.Flag}" }
                        },
                    Resources = new() { Limits = new() { MemoryBytes = config.MemoryLimit * 1024 * 1024, NanoCPUs = config.CPUCount * 1_0000_0000 } }
                },
                EndpointSpec = new()
                {
                    Ports = new PortConfig[]
                    {
                        new() { PublishMode = _meta.ExposePort ? "global" : "vip", TargetPort = (uint)config.ExposedPort }
                    }
                }
            }
        };
}