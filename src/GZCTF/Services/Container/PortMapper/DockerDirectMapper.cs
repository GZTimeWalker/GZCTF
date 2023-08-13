using Docker.DotNet;
using GZCTF.Models.Internal;
using GZCTF.Services.Interface;
using GZCTF.Utils;

namespace GZCTF.Services;

public class DockerDirectMapper : IPortMapper
{
    private readonly ILogger<DockerDirectMapper> _logger;
    private readonly IContainerProvider<DockerClient, DockerMetadata> _provider;
    private readonly DockerMetadata _meta;
    private readonly DockerClient _client;

    public DockerDirectMapper(IContainerProvider<DockerClient, DockerMetadata> provider, ILogger<DockerDirectMapper> logger)
    {
        _logger = logger;
        _provider = provider;
        _meta = _provider.GetMetadata();
        _client = _provider.GetProvider();

        logger.SystemLog($"端口映射方式：Docker 直接端口映射", TaskStatus.Success, LogLevel.Debug);
    }

    public async Task<Container?> MapContainer(Container container, ContainerConfig config, CancellationToken token = default)
    {
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

        var port = info.NetworkSettings.Ports
            .FirstOrDefault(p =>
                p.Key.StartsWith(config.ExposedPort.ToString())
            ).Value.First().HostPort;

        if (int.TryParse(port, out var numport))
            container.Port = numport;
        else
            _logger.SystemLog($"无法转换端口号：{port}，这是非预期的行为", TaskStatus.Failed, LogLevel.Warning);

        container.IP = info.NetworkSettings.IPAddress;

        if (!string.IsNullOrEmpty(_meta.PublicEntry))
            container.PublicIP = _meta.PublicEntry;

        return container;
    }

    // DO NOTHING
    public Task<Container?> UnmapContainer(Container container, CancellationToken token = default)
        => Task.FromResult<Container?>(container);
}

