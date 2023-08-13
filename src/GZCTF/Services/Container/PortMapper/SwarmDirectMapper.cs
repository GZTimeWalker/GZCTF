using Docker.DotNet;
using Docker.DotNet.Models;
using GZCTF.Models.Internal;
using GZCTF.Services.Interface;
using GZCTF.Utils;

namespace GZCTF.Services;

public class SwarmDirectMapper : IPortMapper
{
    private readonly ILogger<SwarmDirectMapper> _logger;
    private readonly IContainerProvider<DockerClient, DockerMetadata> _provider;
    private readonly DockerMetadata _meta;
    private readonly DockerClient _client;

    public SwarmDirectMapper(IContainerProvider<DockerClient, DockerMetadata> provider, ILogger<SwarmDirectMapper> logger)
    {
        _logger = logger;
        _provider = provider;
        _meta = _provider.GetMetadata();
        _client = _provider.GetProvider();

        logger.SystemLog($"端口映射方式：Swarm 直接端口映射", TaskStatus.Success, LogLevel.Debug);
    }

    public async Task<Container?> MapContainer(Container container, ContainerConfig config, CancellationToken token = default)
    {
        var retry = 0;
        SwarmService? res;
        do
        {
            res = await _client.Swarm.InspectServiceAsync(container.ContainerId, token);
            retry++;
            if (retry == 3)
            {
                _logger.SystemLog($"容器 {container.ContainerId} 创建后未获取到端口暴露信息，创建失败", TaskStatus.Failed, LogLevel.Warning);
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

        if (!string.IsNullOrEmpty(_meta.PublicEntry))
            container.PublicIP = _meta.PublicEntry;

        return container;
    }

    // DO NOTHING
    public Task<Container?> UnmapContainer(Container container, CancellationToken token = default)
        => Task.FromResult<Container?>(container);
}

