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
    private readonly DockerMetadata _meta;
    private readonly DockerClient _client;

    public DockerManager(IContainerProvider<DockerClient, DockerMetadata> provider, ILogger<DockerManager> logger)
    {
        _logger = logger;
        _provider = provider;
        _meta = _provider.GetMetadata();
        _client = _provider.GetProvider();

        logger.SystemLog($"容器管理模式：Docker 单实例容器控制", TaskStatus.Success, LogLevel.Debug);
    }


    public async Task DestroyContainerAsync(Container container, CancellationToken token = default)
    {
        try
        {
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

    private CreateContainerParameters GetCreateContainerParameters(ContainerConfig config)
        => new()
        {
            Image = config.Image,
            Labels = new Dictionary<string, string> { ["TeamId"] = config.TeamId, ["UserId"] = config.UserId },
            Name = DockerMetadata.GetName(config),
            Env = config.Flag is null ? Array.Empty<string>() : new string[] { $"GZCTF_FLAG={config.Flag}" },
            HostConfig = new()
            {
                Memory = config.MemoryLimit * 1024 * 1024,
                CPUPercent = config.CPUCount * 10,
                Privileged = config.PrivilegedContainer,
                NetworkMode = _meta.Config.ChallengeNetwork,
            },
        };

    public async Task<Container?> CreateContainerAsync(ContainerConfig config, CancellationToken token = default)
    {
        var parameters = GetCreateContainerParameters(config);

        if (_meta.ExposePort)
        {
            parameters.ExposedPorts = new Dictionary<string, EmptyStruct>()
            {
                [config.ExposedPort.ToString()] = new EmptyStruct()
            };
            parameters.HostConfig.PublishAllPorts = true;
        }

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

        var container = new Container()
        {
            ContainerId = containerRes.ID,
            Image = config.Image,
        };

        var retry = 0;
        bool started;

        do
        {
            started = await _client.Containers.StartContainerAsync(container.ContainerId, new(), token);
            retry++;
            if (retry == 3)
            {
                _logger.SystemLog($"启动容器实例 {container.ContainerId[..12]} ({config.Image.Split("/").LastOrDefault()}) 失败", TaskStatus.Failed, LogLevel.Warning);
                return null;
            }
            if (!started)
                await Task.Delay(500, token);
        } while (!started);

        var info = await _client.Containers.InspectContainerAsync(container.ContainerId, token);

        container.Status = (info.State.Dead || info.State.OOMKilled || info.State.Restarting) ? ContainerStatus.Destroyed :
                info.State.Running ? ContainerStatus.Running : ContainerStatus.Pending;

        if (container.Status != ContainerStatus.Running)
        {
            _logger.SystemLog($"创建 {config.Image.Split("/").LastOrDefault()} 实例遇到错误：{info.State.Error}", TaskStatus.Failed, LogLevel.Warning);
            return null;
        }

        container.StartedAt = DateTimeOffset.Parse(info.State.StartedAt);
        container.ExpectStopAt = container.StartedAt + TimeSpan.FromHours(2);
        container.IP = info.NetworkSettings.Networks.FirstOrDefault().Value.IPAddress;
        container.Port = config.ExposedPort;
        container.IsProxy = !_meta.ExposePort;

        if (_meta.ExposePort)
        {
            var port = info.NetworkSettings.Ports
            .FirstOrDefault(p =>
                p.Key.StartsWith(config.ExposedPort.ToString())
            ).Value.First().HostPort;

            if (int.TryParse(port, out var numport))
                container.PublicPort = numport;
            else
                _logger.SystemLog($"无法转换端口号：{port}，这是非预期的行为", TaskStatus.Failed, LogLevel.Warning);

            if (!string.IsNullOrEmpty(_meta.PublicEntry))
                container.PublicIP = _meta.PublicEntry;
        }

        return container;
    }
}
