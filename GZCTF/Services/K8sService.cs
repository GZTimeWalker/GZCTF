using CTFServer.Models.Internal;
using CTFServer.Services.Interface;
using CTFServer.Utils;
using Docker.DotNet;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using System.Net;
using System.Text;

namespace CTFServer.Services;

public class K8sService : IContainerService
{
    private const string Namespace = "gzctf";

    private readonly ILogger<K8sService> logger;
    private readonly Kubernetes kubernetesClient;
    private readonly string hostIP;
    private readonly string? SecretName;

    public K8sService(IOptions<RegistryConfig> _registry, ILogger<K8sService> logger)
    {
        this.logger = logger;

        if (!File.Exists("k8sconfig.yaml"))
        {
            LogHelper.SystemLog(logger, "无法加载 K8s 配置文件，请确保挂载 /app/k8sconfig.yaml");
            throw new FileNotFoundException("k8sconfig.yaml");
        }

        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile("k8sconfig.yaml");

        hostIP = config.Host[(config.Host.LastIndexOf('/') + 1)..config.Host.LastIndexOf(':')];

        kubernetesClient = new Kubernetes(config);

        if (!kubernetesClient.CoreV1.ListNamespace().Items.Any(ns => ns.Metadata.Name == Namespace))
            kubernetesClient.CoreV1.CreateNamespace(new() { Metadata = new() { Name = Namespace } });

        if (!string.IsNullOrWhiteSpace(_registry.Value.ServerAddress)
            && !string.IsNullOrWhiteSpace(_registry.Value.UserName)
            && !string.IsNullOrWhiteSpace(_registry.Value.Password))
        {
            var padding = Codec.StrMD5($"{_registry.Value.UserName}@{_registry.Value.Password}@{_registry.Value.ServerAddress}");
            SecretName = $"{_registry.Value.UserName}-{padding}";

            var auth = Codec.Base64.Encode($"{_registry.Value.UserName}:{_registry.Value.Password}");
            var dockerjson = Codec.Base64.EncodeToBytes(
                $"{{\"auths\":" +
                    $"{{\"{_registry.Value.ServerAddress}\":" +
                        $"{{\"auth\":\"{auth}\"," +
                        $"\"username\":\"{_registry.Value.UserName}\"," +
                        $"\"password\":\"{_registry.Value.Password}\"" +
                $"}}}}}}");
            var secret = new V1Secret()
            {
                Metadata = new V1ObjectMeta()
                {
                    Name = SecretName,
                    NamespaceProperty = Namespace,
                },
                Data = new Dictionary<string, byte[]>() {
                    {".dockerconfigjson", dockerjson}
                },
                Type = "kubernetes.io/dockerconfigjson"
            };

            try
            {
                kubernetesClient.CoreV1.ReplaceNamespacedSecret(secret, SecretName, Namespace);
            }
            catch (Exception)
            {
                kubernetesClient.CoreV1.CreateNamespacedSecret(secret, Namespace);
            }
        }

        logger.SystemLog($"K8s 服务已启动 ({config.Host})", TaskStatus.Success, LogLevel.Debug);
    }

    public async Task<Container?> CreateContainer(ContainerConfig config, CancellationToken token = default)
    {
        // use uuid avoid conflict
        var name = $"{config.Image.Split("/").LastOrDefault()?.Split(":").FirstOrDefault()}-{Guid.NewGuid().ToString("N")[..16]}"
            .Replace('_', '-'); // ensure name is available

        var pod = new V1Pod("v1", "Pod")
        {
            Metadata = new V1ObjectMeta()
            {
                Name = name,
                NamespaceProperty = Namespace,
                Labels = new Dictionary<string, string>()
                {
                    { "ctf.gzti.me/ResourceId", name },
                    { "ctf.gzti.me/TeamId", config.TeamId },
                    { "ctf.gzti.me/UserId", config.UserId }
                }
            },
            Spec = new V1PodSpec()
            {
                ImagePullSecrets = SecretName is null ?
                    Array.Empty<V1LocalObjectReference>() :
                    new List<V1LocalObjectReference>() { new() { Name = SecretName } },
                Containers = new[]
                {
                    new V1Container()
                    {
                        Name = name,
                        Image = config.Image,
                        ImagePullPolicy = "Always",
                        Env = config.Flag is null ? new List<V1EnvVar>() : new[]
                        {
                            new V1EnvVar("GZCTF_FLAG", config.Flag)
                        },
                        Ports = new[]
                        {
                            new V1ContainerPort(config.ExposedPort)
                        },
                        Resources = new V1ResourceRequirements()
                        {
                            Limits = new Dictionary<string, ResourceQuantity>()
                            {
                                { "cpu", new ResourceQuantity($"{config.CPUCount}")},
                                { "memory", new ResourceQuantity($"{config.MemoryLimit}Mi") }
                            },
                            Requests = new Dictionary<string, ResourceQuantity>()
                            {
                                { "cpu", new ResourceQuantity("1")},
                                { "memory", new ResourceQuantity("32Mi") }
                            },
                        }
                    }
                },
                RestartPolicy = "Never"
            }
        };

        try
        {
            pod = await kubernetesClient.CreateNamespacedPodAsync(pod, Namespace, cancellationToken: token);
        }
        catch (HttpOperationException e)
        {
            logger.SystemLog($"容器 {name} 创建失败, 状态：{e.Response.StatusCode.ToString()}", TaskStatus.Fail, LogLevel.Warning);
            logger.SystemLog($"容器 {name} 创建失败, 响应：{e.Response.Content}", TaskStatus.Fail, LogLevel.Error);
            return null;
        }
        catch (Exception e)
        {
            logger.LogError(e, "创建容器失败");
            return null;
        }

        if (pod is null)
        {
            logger.SystemLog($"创建容器实例 {config.Image.Split("/").LastOrDefault()} 失败", TaskStatus.Fail, LogLevel.Warning);
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
                NamespaceProperty = Namespace,
                Labels = new Dictionary<string, string>()
                {
                    { "ctf.gzti.me/ResourceId", name }
                }
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
                    { "ctf.gzti.me/ResourceId", name }
                }
            }
        };

        try
        {
            service = await kubernetesClient.CoreV1.CreateNamespacedServiceAsync(service, Namespace, cancellationToken: token);
        }
        catch (Exception e)
        {
            logger.LogError(e, "创建服务失败");
            return null;
        }

        container.PublicPort = service.Spec.Ports[0].NodePort;
        container.PublicIP = hostIP;
        container.StartedAt = DateTimeOffset.UtcNow;

        return container;
    }

    public async Task DestroyContainer(Container container, CancellationToken token = default)
    {
        try
        {
            await kubernetesClient.CoreV1.DeleteNamespacedServiceAsync(container.ContainerId, Namespace, cancellationToken: token);
            await kubernetesClient.CoreV1.DeleteNamespacedPodAsync(container.ContainerId, Namespace, cancellationToken: token);
        }
        catch (HttpOperationException e)
        {
            if (e.Response.StatusCode == HttpStatusCode.NotFound)
            {
                container.Status = ContainerStatus.Destroyed;
                return;
            }
            logger.SystemLog($"容器 {container.ContainerId} 删除失败, 状态：{e.Response.StatusCode.ToString()}", TaskStatus.Fail, LogLevel.Warning);
            logger.SystemLog($"容器 {container.ContainerId} 删除失败, 响应：{e.Response.Content}", TaskStatus.Fail, LogLevel.Error);
        }
        catch (Exception e)
        {
            logger.LogError(e, "删除容器失败");
            return;
        }

        container.Status = ContainerStatus.Destroyed;
    }
}