using System.Net;
using Docker.DotNet;
using Docker.DotNet.Models;
using GZCTF.Models.Internal;
using GZCTF.Services.Interface;
using GZCTF.Utils;

namespace GZCTF.Services;

public class DockerManager : IContainerManager
{
    private readonly ILogger<DockerManager> _logger;
    private readonly IContainerProvider<DockerClient, DockerMetadata> _provider;
    private readonly IPortMapper _mapper;
    private readonly DockerMetadata _meta;
    private readonly DockerClient _client;

    public DockerManager(IContainerProvider<DockerClient, DockerMetadata> provider, IPortMapper mapper, ILogger<DockerManager> logger)
    {
        _logger = logger;
        _mapper = mapper;
        _provider = provider;
        _meta = _provider.GetMetadata();
        _client = _provider.GetProvider();

        logger.SystemLog($"容器管理模式：Docker 单实例容器控制", TaskStatus.Success, LogLevel.Debug);
    }


    public async Task DestroyContainerAsync(Container container, CancellationToken token = default)
    {
        try
        {
            await _mapper.UnmapContainer(container, token);
            await _client.Containers.RemoveContainerAsync(container.ContainerId, new() { Force = true }, token);
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

    private static CreateContainerParameters GetCreateContainerParameters(ContainerConfig config)
        => new()
        {
            Image = config.Image,
            Labels = new Dictionary<string, string> { ["TeamId"] = config.TeamId, ["UserId"] = config.UserId },
            Name = DockerMetadata.GetName(config),
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

    public async Task<Container?> CreateContainerAsync(ContainerConfig config, CancellationToken token = default)
    {
        var parameters = GetCreateContainerParameters(config);
        CreateContainerResponse? containerRes = null;
        try
        {
            containerRes = await _client.Containers.CreateContainerAsync(parameters, token);
        }
        catch (DockerImageNotFoundException)
        {
            _logger.SystemLog($"拉取容器镜像 {config.Image}", TaskStatus.Pending, LogLevel.Information);

            await _client.Images.CreateImageAsync(new()
            {
                FromImage = config.Image
            }, _meta.Auth, new Progress<JSONMessage>(msg =>
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
            containerRes ??= await _client.Containers.CreateContainerAsync(parameters, token);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"容器 {parameters.Name} 创建失败");
            return null;
        }

        return await _mapper.MapContainer(new()
        {
            ContainerId = containerRes.ID,
            Image = config.Image,
        }, config, token);
    }
}
