using CTFServer.Models.Internal;
using CTFServer.Services.Interface;
using CTFServer.Utils;
using Docker.DotNet.Models;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Options;
using System.Text;

namespace CTFServer.Services;

public class K8sService : IContainerService
{
    private readonly ILogger<K8sService> logger;
    private readonly Kubernetes kubernetesClient;
    private readonly string HostIP;

    public K8sService(ILogger<K8sService> logger)
    {
        this.logger = logger;

        if (!File.Exists("k8sconfig.yaml"))
        {
            LogHelper.SystemLog(logger, "无法加载 K8s 配置文件，请确保挂载 /app/k8sconfig.yaml");
            throw new FileNotFoundException("k8sconfig.yaml");
        }

        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile("k8sconfig.yaml");

        HostIP = config.Host[(config.Host.LastIndexOf('/') + 1)..config.Host.LastIndexOf(':')];

        kubernetesClient = new Kubernetes(config);

        if (!kubernetesClient.CoreV1.ListNamespace().Items.Any(ns => ns.Metadata.Name == "gzctf"))
        {
            kubernetesClient.CoreV1.CreateNamespace(new()
            {
                Metadata = new()
                {
                    Name = "gzctf"
                }
            });
        }

        logger.SystemLog($"K8s 服务已启动 ({config.Host})", TaskStatus.Success, LogLevel.Debug);
    }

    public async Task<Container?> CreateContainer(ContainerConfig config, CancellationToken token = default)
    {
        var name = $"{config.Image.Split("/").LastOrDefault()}-{Codec.StrMD5(config.Flag ?? Guid.NewGuid().ToString())[..16]}";
        var pod = new V1Pod("v1", "Pod")
        {
            Metadata = new V1ObjectMeta()
            {
                Name = name,
                NamespaceProperty = "gzctf",
                Labels = new Dictionary<string, string>()
                {
                    { "ctf.gzti.me/ResourceId", name }
                }
            },
            Spec = new V1PodSpec()
            {
                Containers = new[]
                {
                    new V1Container()
                    {
                        Name = name,
                        Image = config.Image,
                        Env = new[]
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
            pod = await kubernetesClient.CreateNamespacedPodAsync(pod, "gzctf", cancellationToken: token);
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
                NamespaceProperty = "gzctf",
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
            service = await kubernetesClient.CreateNamespacedServiceAsync(service, "gzctf", cancellationToken: token);
        }
        catch (Exception e)
        {
            logger.LogError(e, "创建服务失败");
            return null;
        }

        container.PublicPort = service.Spec.Ports[0].NodePort;
        container.PublicIP = HostIP;
        container.StartedAt = DateTimeOffset.UtcNow;

        return container;
    }

    public async Task DestoryContainer(Container container, CancellationToken token = default)
    {
        try
        {
            await kubernetesClient.CoreV1.DeleteNamespacedServiceAsync(container.ContainerId, "gzctf", cancellationToken: token);
            await kubernetesClient.CoreV1.DeleteNamespacedPodAsync(container.ContainerId, "gzctf", cancellationToken: token);
        }
        catch (Exception e)
        {
            logger.LogError(e, "删除容器失败");
            return;
        }

        container.Status = ContainerStatus.Destoryed;
    }

    public async Task<IList<ContainerInfo>> GetContainers(CancellationToken token = default)
    {
        var pods = await kubernetesClient.ListNamespacedPodAsync("gzctf", cancellationToken: token);
        return (from pod in pods.Items
                select new ContainerInfo
                {
                    Id = pod.Metadata.Name,
                    Name = pod.Metadata.Name,
                    Image = pod.Spec.Containers[0].Image,
                    State = pod.Status.Message
                }).ToArray();
    }

    public async Task<string> GetHostInfo(CancellationToken token = default)
    {
        var nodes = await kubernetesClient.ListNodeAsync(cancellationToken: token);
        StringBuilder builder = new();

        builder.AppendLine("[[ K8s Nodes ]]");
        foreach (var node in nodes.Items)
        {
            builder.AppendLine($"[{node.Metadata.Name}]");
            foreach (var item in node.Status.Capacity)
            {
                builder.AppendLine($"{item.Key,-20}: {item.Value}");
            }
            foreach (var item in node.Status.Conditions)
            {
                builder.AppendLine($"{item.Type,-20}: {item.Status}");
            }
            builder.AppendLine($"{"Addresses",-20}: ");
            foreach (var item in node.Status.Addresses)
            {
                builder.AppendLine($"{"",-22}{item.Address}({item.Type})");
            }
        }

        return builder.ToString();
    }

    public async Task<Container> QueryContainer(Container container, CancellationToken token = default)
    {
        var pod = await kubernetesClient.ReadNamespacedPodAsync(container.ContainerId, "gzctf", cancellationToken: token);

        if (pod is null)
        {
            container.Status = ContainerStatus.Destoryed;
            return container;
        }

        container.Status = pod.Status.Phase == "Succeeded" ? ContainerStatus.Running : ContainerStatus.Pending;
        container.IP = pod.Status.PodIP;

        return container;
    }
}