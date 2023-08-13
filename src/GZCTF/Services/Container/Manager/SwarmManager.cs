using System.Net;
using Docker.DotNet;
using Docker.DotNet.Models;
using GZCTF.Models.Internal;
using GZCTF.Services.Interface;
using GZCTF.Utils;

namespace GZCTF.Services;

public class SwarmManager : IContainerManager
{
    private readonly ILogger<SwarmManager> _logger;
    private readonly IContainerProvider<DockerClient, DockerMetadata> _provider;
    private readonly IPortMapper _mapper;
    private readonly DockerMetadata _meta;
    private readonly DockerClient _client;

    public SwarmManager(IContainerProvider<DockerClient, DockerMetadata> provider, IPortMapper mapper, ILogger<SwarmManager> logger)
    {
        _logger = logger;
        _mapper = mapper;
        _provider = provider;
        _meta = _provider.GetMetadata();
        _client = _provider.GetProvider();

        logger.SystemLog($"容器管理模式：Docker Swarm 集群容器控制", TaskStatus.Success, LogLevel.Debug);
    }

    public async Task DestroyContainerAsync(Container container, CancellationToken token = default)
    {
        try
        {
            await _mapper.UnmapContainer(container, token);
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

    private ServiceCreateParameters GetServiceCreateParameters(ContainerConfig config)
        => new()
        {
            RegistryAuth = _meta.Auth,
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

    public async Task<Container?> CreateContainerAsync(ContainerConfig config, CancellationToken token = default)
    {
        var parameters = GetServiceCreateParameters(config);
        int retry = 0;
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
                _logger.SystemLog($"容器 {parameters.Service.Name} 已存在，尝试移除后重新创建", TaskStatus.Duplicate, LogLevel.Warning);
                await _client.Swarm.RemoveServiceAsync(parameters.Service.Name, token);
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

        return await _mapper.MapContainer(new()
        {
            ContainerId = serviceRes.ID,
            Image = config.Image,
        }, config, token);
    }
}
