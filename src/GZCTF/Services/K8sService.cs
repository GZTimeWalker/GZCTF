using System.Net;
using System.Text;
using System.Text.Json;
using GZCTF.Models.Internal;
using GZCTF.Services.Interface;
using GZCTF.Utils;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Options;

namespace GZCTF.Services;

public class K8sService : IContainerService
{
    private const string NetworkPolicy = "gzctf-policy";

    private readonly ILogger<K8sService> _logger;
    private readonly Kubernetes _kubernetesClient;
    private readonly string _hostIP;
    private readonly string _publicEntry;
    private readonly string? _authSecretName;
    private readonly K8sConfig _options;

    public K8sService(IOptions<RegistryConfig> registry, IOptions<ContainerProvider> provider, ILogger<K8sService> logger)
    {
        _logger = logger;
        _publicEntry = provider.Value.PublicEntry;
        _options = provider.Value.K8sConfig ?? new();

        if (!File.Exists(_options.KubeConfig))
        {
            LogHelper.SystemLog(logger, $"无法加载 K8s 配置文件，请确保配置文件存在 {_options.KubeConfig}");
            throw new FileNotFoundException(_options.KubeConfig);
        }

        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(_options.KubeConfig);

        _hostIP = config.Host[(config.Host.LastIndexOf('/') + 1)..config.Host.LastIndexOf(':')];

        _kubernetesClient = new Kubernetes(config);

        var registryValue = registry.Value;
        var withAuth = !string.IsNullOrWhiteSpace(registryValue.ServerAddress)
            && !string.IsNullOrWhiteSpace(registryValue.UserName)
            && !string.IsNullOrWhiteSpace(registryValue.Password);

        if (withAuth)
        {
            var padding = $"{registryValue.UserName}@{registryValue.Password}@{registryValue.ServerAddress}".StrMD5();
            _authSecretName = $"{registryValue.UserName}-{padding}".ToValidRFC1123String("secret");
        }

        try
        {
            InitK8s(withAuth, registryValue);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"K8s 初始化失败，请检查相关配置是否正确 ({config.Host})");
            Program.ExitWithFatalMessage($"K8s 初始化失败，请检查相关配置是否正确 ({config.Host})");
        }

        logger.SystemLog($"K8s 服务已启动 ({config.Host})", TaskStatus.Success, LogLevel.Debug);
    }

    public async Task<Container?> CreateContainerAsync(ContainerConfig config, CancellationToken token = default)
    {
        var imageName = config.Image.Split("/").LastOrDefault()?.Split(":").FirstOrDefault();

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
            pod = await _kubernetesClient.CreateNamespacedPodAsync(pod, _options.Namespace, cancellationToken: token);
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

        var container = new Container()
        {
            ContainerId = name,
            Image = config.Image,
            Port = config.ExposedPort,
            IsProxy = true,
        };

        var service = new V1Service("v1", "Service")
        {
            Metadata = new V1ObjectMeta()
            {
                Name = name,
                NamespaceProperty = _options.Namespace,
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
            service = await _kubernetesClient.CoreV1.CreateNamespacedServiceAsync(service, _options.Namespace, cancellationToken: token);
        }
        catch (HttpOperationException e)
        {
            try
            {
                // remove the pod if service creation failed, ignore the error
                await _kubernetesClient.CoreV1.DeleteNamespacedPodAsync(name, _options.Namespace, cancellationToken: token);
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
                await _kubernetesClient.CoreV1.DeleteNamespacedPodAsync(name, _options.Namespace, cancellationToken: token);
            }
            catch { }
            _logger.LogError(e, "创建服务失败");
            return null;
        }

        container.PublicPort = service.Spec.Ports[0].NodePort;
        container.IP = _hostIP;
        container.PublicIP = _publicEntry;
        container.StartedAt = DateTimeOffset.UtcNow;

        return container;
    }

    public async Task DestroyContainerAsync(Container container, CancellationToken token = default)
    {
        try
        {
            await _kubernetesClient.CoreV1.DeleteNamespacedServiceAsync(container.ContainerId, _options.Namespace, cancellationToken: token);
            await _kubernetesClient.CoreV1.DeleteNamespacedPodAsync(container.ContainerId, _options.Namespace, cancellationToken: token);
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

    private void InitK8s(bool withAuth, RegistryConfig? registry)
    {
        if (_kubernetesClient.CoreV1.ListNamespace().Items.All(ns => ns.Metadata.Name != _options.Namespace))
            _kubernetesClient.CoreV1.CreateNamespace(new() { Metadata = new() { Name = _options.Namespace } });

        if (_kubernetesClient.NetworkingV1.ListNamespacedNetworkPolicy(_options.Namespace).Items.All(np => np.Metadata.Name != NetworkPolicy))
        {

            _kubernetesClient.NetworkingV1.CreateNamespacedNetworkPolicy(new()
            {
                Metadata = new() { Name = NetworkPolicy },
                Spec = new()
                {
                    PodSelector = new(),
                    PolicyTypes = new[] { "Egress" },
                    Egress = new[]
                    {
                        new V1NetworkPolicyEgressRule()
                        {
                            To = new[]
                            {
                                new V1NetworkPolicyPeer() {
                                    IpBlock = new() {
                                        Cidr = "0.0.0.0/0",
                                        // FIXME: remove nullable when JsonObjectCreationHandling release
                                        Except = _options.AllowCIDR ?? new[] { "10.0.0.0/8" }
                                    }
                                },
                            }
                        }
                    }
                }
            }, _options.Namespace);
        }

        if (withAuth && registry is not null && registry.ServerAddress is not null)
        {
            var auth = Codec.Base64.Encode($"{registry.UserName}:{registry.Password}");
            var dockerjsonObj = new
            {
                auths = new Dictionary<string, object> {
                    {
                        registry.ServerAddress, new {
                            auth,
                            username = registry.UserName,
                            password = registry.Password
                        }
                    }
                }
            };
            var dockerjsonBytes = JsonSerializer.SerializeToUtf8Bytes(dockerjsonObj);
            var secret = new V1Secret()
            {
                Metadata = new V1ObjectMeta()
                {
                    Name = _authSecretName,
                    NamespaceProperty = _options.Namespace,
                },
                Data = new Dictionary<string, byte[]>() { [".dockerconfigjson"] = dockerjsonBytes },
                Type = "kubernetes.io/dockerconfigjson"
            };

            try
            {
                _kubernetesClient.CoreV1.ReplaceNamespacedSecret(secret, _authSecretName, _options.Namespace);
            }
            catch
            {
                _kubernetesClient.CoreV1.CreateNamespacedSecret(secret, _options.Namespace);
            }
        }
    }
}
