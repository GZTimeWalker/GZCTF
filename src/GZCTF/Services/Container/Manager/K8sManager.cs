using System.Diagnostics.CodeAnalysis;
using System.Net;
using GZCTF.Models.Internal;
using GZCTF.Services.Container.Provider;
using GZCTF.Services.Interface;
using k8s;
using k8s.Autorest;
using k8s.Models;

namespace GZCTF.Services.Container.Manager;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class K8sManager : IContainerManager
{
    readonly Kubernetes _client;
    readonly ILogger<K8sManager> _logger;
    readonly K8sMetadata _meta;

    public K8sManager(IContainerProvider<Kubernetes, K8sMetadata> provider, ILogger<K8sManager> logger)
    {
        _logger = logger;
        _meta = provider.GetMetadata();
        _client = provider.GetProvider();

        logger.SystemLog("容器管理模式：K8s 集群 Pod 容器控制", TaskStatus.Success, LogLevel.Debug);
    }

    public async Task<Models.Data.Container?> CreateContainerAsync(ContainerConfig config,
        CancellationToken token = default)
    {
        var imageName = config.Image.Split("/").LastOrDefault()?.Split(":").FirstOrDefault();
        var authSecretName = _meta.AuthSecretName;
        K8sConfig options = _meta.Config;

        if (imageName is null)
        {
            _logger.SystemLog($"无法解析镜像名称 {config.Image}", TaskStatus.Failed, LogLevel.Warning);
            return null;
        }

        var name = $"{imageName.ToValidRFC1123String("chal")}-{Guid.NewGuid().ToString("N")[..16]}";

        var pod = new V1Pod("v1", "Pod")
        {
            Metadata = new V1ObjectMeta
            {
                Name = name,
                NamespaceProperty = options.Namespace,
                Labels = new Dictionary<string, string>
                {
                    ["ctf.gzti.me/ResourceId"] = name,
                    ["ctf.gzti.me/TeamId"] = config.TeamId,
                    ["ctf.gzti.me/UserId"] = config.UserId
                }
            },
            Spec = new V1PodSpec
            {
                ImagePullSecrets =
                    authSecretName is null
                        ? Array.Empty<V1LocalObjectReference>()
                        : new List<V1LocalObjectReference> { new() { Name = authSecretName } },
                DnsPolicy = "None",
                DnsConfig = new()
                {
                    // FIXME: remove nullable when JsonObjectCreationHandling release
                    Nameservers = options.DNS ?? new[] { "8.8.8.8", "223.5.5.5", "114.114.114.114" }
                },
                EnableServiceLinks = false,
                Containers = new[]
                {
                    new V1Container
                    {
                        Name = name,
                        Image = config.Image,
                        ImagePullPolicy = "Always",
                        Env =
                            config.Flag is null
                                ? new List<V1EnvVar>()
                                : new[] { new V1EnvVar("GZCTF_FLAG", config.Flag) },
                        Ports = new[] { new V1ContainerPort(config.ExposedPort) },
                        Resources = new V1ResourceRequirements
                        {
                            Limits = new Dictionary<string, ResourceQuantity>
                            {
                                ["cpu"] = new($"{config.CPUCount * 100}m"),
                                ["memory"] = new($"{config.MemoryLimit}Mi"),
                                ["ephemeral-storage"] = new($"{config.StorageLimit}Mi")
                            },
                            Requests = new Dictionary<string, ResourceQuantity> { ["cpu"] = new("10m"), ["memory"] = new("32Mi") }
                        }
                    }
                },
                RestartPolicy = "Never"
            }
        };

        try
        {
            pod = await _client.CreateNamespacedPodAsync(pod, options.Namespace, cancellationToken: token);
        }
        catch (HttpOperationException e)
        {
            _logger.SystemLog($"容器 {name} 创建失败, 状态：{e.Response.StatusCode}", TaskStatus.Failed, LogLevel.Warning);
            _logger.SystemLog($"容器 {name} 创建失败, 响应：{e.Response.Content}", TaskStatus.Failed, LogLevel.Error);
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "创建容器失败");
            return null;
        }

        if (pod is null)
        {
            _logger.SystemLog($"创建容器实例 {config.Image.Split("/").LastOrDefault()} 失败", TaskStatus.Failed,
                LogLevel.Warning);
            return null;
        }

        // Service is needed for port mapping
        var container = new Models.Data.Container { ContainerId = name, Image = config.Image, Port = config.ExposedPort };

        var service = new V1Service("v1", "Service")
        {
            Metadata = new V1ObjectMeta
            {
                Name = name,
                NamespaceProperty = _meta.Config.Namespace,
                Labels = new Dictionary<string, string> { ["ctf.gzti.me/ResourceId"] = name }
            },
            Spec = new V1ServiceSpec
            {
                Type = _meta.ExposePort ? "NodePort" : "ClusterIP",
                Ports = new[] { new V1ServicePort(config.ExposedPort, targetPort: config.ExposedPort) },
                Selector = new Dictionary<string, string> { ["ctf.gzti.me/ResourceId"] = name }
            }
        };

        try
        {
            service = await _client.CoreV1.CreateNamespacedServiceAsync(service, _meta.Config.Namespace,
                cancellationToken: token);
        }
        catch (HttpOperationException e)
        {
            try
            {
                // remove the pod if service creation failed, ignore the error
                await _client.CoreV1.DeleteNamespacedPodAsync(name, _meta.Config.Namespace, cancellationToken: token);
            }
            catch
            {
                // ignored
            }

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
            catch
            {
                // ignored
            }

            _logger.LogError(e, "创建服务失败");
            return null;
        }

        container.StartedAt = DateTimeOffset.UtcNow;
        container.ExpectStopAt = container.StartedAt + TimeSpan.FromHours(2);
        container.IP = service.Spec.ClusterIP;
        container.Port = config.ExposedPort;
        container.IsProxy = !_meta.ExposePort;

        if (_meta.ExposePort)
        {
            container.PublicIP = _meta.PublicEntry;
            container.PublicPort = service.Spec.Ports[0].NodePort;
        }

        return container;
    }

    public async Task DestroyContainerAsync(Models.Data.Container container, CancellationToken token = default)
    {
        try
        {
            await _client.CoreV1.DeleteNamespacedServiceAsync(container.ContainerId, _meta.Config.Namespace,
                cancellationToken: token);
            await _client.CoreV1.DeleteNamespacedPodAsync(container.ContainerId, _meta.Config.Namespace,
                cancellationToken: token);
        }
        catch (HttpOperationException e)
        {
            if (e.Response.StatusCode == HttpStatusCode.NotFound)
            {
                container.Status = ContainerStatus.Destroyed;
                return;
            }

            _logger.SystemLog($"容器 {container.ContainerId} 删除失败, 状态：{e.Response.StatusCode}", TaskStatus.Failed,
                LogLevel.Warning);
            _logger.SystemLog($"容器 {container.ContainerId} 删除失败, 响应：{e.Response.Content}", TaskStatus.Failed,
                LogLevel.Error);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "删除容器失败");
            return;
        }

        container.Status = ContainerStatus.Destroyed;
    }
}