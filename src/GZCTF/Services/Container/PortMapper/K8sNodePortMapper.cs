using GZCTF.Models.Internal;
using GZCTF.Services.Interface;
using GZCTF.Utils;
using k8s;
using k8s.Autorest;
using k8s.Models;

namespace GZCTF.Services;

public class K8sNodePortMapper : IPortMapper
{
    private readonly ILogger<K8sNodePortMapper> _logger;
    private readonly IContainerProvider<Kubernetes, K8sMetadata> _provider;
    private readonly K8sMetadata _meta;
    private readonly Kubernetes _client;

    public K8sNodePortMapper(IContainerProvider<Kubernetes, K8sMetadata> provider, ILogger<K8sNodePortMapper> logger)
    {
        _logger = logger;
        _provider = provider;
        _meta = _provider.GetMetadata();
        _client = _provider.GetProvider();

        logger.SystemLog($"端口映射方式：K8s NodePort 服务端口映射", TaskStatus.Success, LogLevel.Debug);
    }

    public async Task<Container?> MapContainer(Container container, ContainerConfig config, CancellationToken token = default)
    {
        var name = container.ContainerId;

        var service = new V1Service("v1", "Service")
        {
            Metadata = new V1ObjectMeta()
            {
                Name = name,
                NamespaceProperty = _meta.Config.Namespace,
                Labels = new Dictionary<string, string>() { ["ctf.gzti.me/ResourceId"] = name }
            },
            Spec = new V1ServiceSpec()
            {
                Type = "NodePort",
                Ports = new[]
                {
                    new V1ServicePort(config.ExposedPort, targetPort: config.ExposedPort)
                },
                Selector = new Dictionary<string, string>()
                {
                    ["ctf.gzti.me/ResourceId"] = name
                }
            }
        };

        try
        {
            service = await _client.CoreV1.CreateNamespacedServiceAsync(service, _meta.Config.Namespace, cancellationToken: token);
        }
        catch (HttpOperationException e)
        {
            try
            {
                // remove the pod if service creation failed, ignore the error
                await _client.CoreV1.DeleteNamespacedPodAsync(name, _meta.Config.Namespace, cancellationToken: token);
            }
            catch { }
            _logger.SystemLog($"服务 {name} 创建失败, 状态：{e.Response.StatusCode}", TaskStatus.Failed, LogLevel.Warning);
            _logger.SystemLog($"服务 {name} 创建失败, 响应：{e.Response.Content}", TaskStatus.Failed, LogLevel.Error);
            return null;
        }
        catch (Exception e)
        {
            try
            {
                // remove the pod if service creation failed, ignore the error
                await _client.CoreV1.DeleteNamespacedPodAsync(name, _meta.Config.Namespace, cancellationToken: token);
            }
            catch { }
            _logger.LogError(e, "创建服务失败");
            return null;
        }

        container.PublicPort = service.Spec.Ports[0].NodePort;
        container.IP = _meta.HostIP;
        container.PublicIP = _meta.PublicEntry;
        container.StartedAt = DateTimeOffset.UtcNow;

        return container;
    }

    public async Task<Container?> UnmapContainer(Container container, CancellationToken token = default)
    {
        await _client.CoreV1.DeleteNamespacedServiceAsync(container.ContainerId, _meta.Config.Namespace, cancellationToken: token);
        return container;
    }
}

