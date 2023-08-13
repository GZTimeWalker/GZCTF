using System.Net;
using GZCTF.Models.Internal;
using GZCTF.Services.Interface;
using GZCTF.Utils;
using k8s;
using k8s.Autorest;
using k8s.Models;

namespace GZCTF.Services;

public class K8sManager : IContainerManager
{
    private readonly ILogger<K8sManager> _logger;
    private readonly IContainerProvider<Kubernetes, K8sMetadata> _provider;
    private readonly IPortMapper _mapper;
    private readonly K8sMetadata _meta;
    private readonly Kubernetes _client;

    public K8sManager(IContainerProvider<Kubernetes, K8sMetadata> provider, IPortMapper mapper, ILogger<K8sManager> logger)
    {
        _logger = logger;
        _mapper = mapper;
        _provider = provider;
        _meta = _provider.GetMetadata();
        _client = _provider.GetProvider();

        logger.SystemLog($"容器管理模式：K8s 集群 Pod 容器控制", TaskStatus.Success, LogLevel.Debug);
    }

    public async Task<Container?> CreateContainerAsync(ContainerConfig config, CancellationToken token = default)
    {
        var imageName = config.Image.Split("/").LastOrDefault()?.Split(":").FirstOrDefault();
        var _authSecretName = _meta.AuthSecretName;
        var _options = _meta.Config;

        if (imageName is null)
        {
            _logger.SystemLog($"无法解析镜像名称 {config.Image}", TaskStatus.Failed, LogLevel.Warning);
            return null;
        }

        var name = $"{imageName.ToValidRFC1123String("chal")}-{Guid.NewGuid().ToString("N")[..16]}";

        var pod = new V1Pod("v1", "Pod")
        {
            Metadata = new V1ObjectMeta()
            {
                Name = name,
                NamespaceProperty = _options.Namespace,
                Labels = new Dictionary<string, string>()
                {
                    ["ctf.gzti.me/ResourceId"] = name,
                    ["ctf.gzti.me/TeamId"] = config.TeamId,
                    ["ctf.gzti.me/UserId"] = config.UserId
                }
            },
            Spec = new V1PodSpec()
            {
                ImagePullSecrets = _authSecretName is null ?
                    Array.Empty<V1LocalObjectReference>() :
                    new List<V1LocalObjectReference>() { new() { Name = _authSecretName } },
                DnsPolicy = "None",
                DnsConfig = new()
                {
                    // FIXME: remove nullable when JsonObjectCreationHandling release
                    Nameservers = _options.DNS ?? new[] { "8.8.8.8", "223.5.5.5", "114.114.114.114" },
                },
                EnableServiceLinks = false,
                Containers = new[]
                {
                    new V1Container()
                    {
                        Name = name,
                        Image = config.Image,
                        ImagePullPolicy = "Always",
                        SecurityContext = new() { Privileged = config.PrivilegedContainer },
                        Env = config.Flag is null ? new List<V1EnvVar>() : new[]
                        {
                            new V1EnvVar("GZCTF_FLAG", config.Flag)
                        },
                        Ports = new[] { new V1ContainerPort(config.ExposedPort) },
                        Resources = new V1ResourceRequirements()
                        {
                            Limits = new Dictionary<string, ResourceQuantity>()
                            {
                                ["cpu"] = new ResourceQuantity($"{config.CPUCount * 100}m"),
                                ["memory"] = new ResourceQuantity($"{config.MemoryLimit}Mi"),
                                ["ephemeral-storage"] = new ResourceQuantity($"{config.StorageLimit}Mi")
                            },
                            Requests = new Dictionary<string, ResourceQuantity>()
                            {
                                ["cpu"] = new ResourceQuantity("10m"),
                                ["memory"] = new ResourceQuantity("32Mi")
                            }
                        }
                    }
                },
                RestartPolicy = "Never"
            }
        };

        try
        {
            pod = await _client.CreateNamespacedPodAsync(pod, _options.Namespace, cancellationToken: token);
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
            _logger.SystemLog($"创建容器实例 {config.Image.Split("/").LastOrDefault()} 失败", TaskStatus.Failed, LogLevel.Warning);
            return null;
        }


        return await _mapper.MapContainer(new Container()
        {
            ContainerId = name,
            Image = config.Image,
            Port = config.ExposedPort,
        }, config, token);
    }

    public async Task DestroyContainerAsync(Container container, CancellationToken token = default)
    {
        try
        {
            await _mapper.UnmapContainer(container, token);
            await _client.CoreV1.DeleteNamespacedPodAsync(container.ContainerId, _meta.Config.Namespace, cancellationToken: token);
        }
        catch (HttpOperationException e)
        {
            if (e.Response.StatusCode == HttpStatusCode.NotFound)
            {
                container.Status = ContainerStatus.Destroyed;
                return;
            }
            _logger.SystemLog($"容器 {container.ContainerId} 删除失败, 状态：{e.Response.StatusCode}", TaskStatus.Failed, LogLevel.Warning);
            _logger.SystemLog($"容器 {container.ContainerId} 删除失败, 响应：{e.Response.Content}", TaskStatus.Failed, LogLevel.Error);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "删除容器失败");
            return;
        }

        container.Status = ContainerStatus.Destroyed;
    }

}