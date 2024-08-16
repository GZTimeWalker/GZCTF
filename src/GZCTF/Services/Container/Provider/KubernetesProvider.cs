using System.Text.Json;
using GZCTF.Models.Internal;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Options;
using static GZCTF.Program;

namespace GZCTF.Services.Container.Provider;

public class KubernetesMetadata : ContainerProviderMetadata
{
    /// <summary>
    /// 容器注册表鉴权 Secret 名称
    /// </summary>
    public string? AuthSecretName { get; set; }

    /// <summary>
    /// K8s 集群 Host IP
    /// </summary>
    public string HostIp { get; set; } = string.Empty;

    /// <summary>
    /// K8s 配置
    /// </summary>
    public KubernetesConfig Config { get; set; } = new();
}

public class KubernetesProvider : IContainerProvider<Kubernetes, KubernetesMetadata>
{
    const string NetworkPolicy = "gzctf-policy";

    readonly Kubernetes _kubernetesClient;
    readonly KubernetesMetadata _kubernetesMetadata;

    public KubernetesProvider(IOptions<RegistryConfig> registry, IOptions<ContainerProvider> options,
        ILogger<KubernetesProvider> logger)
    {
        _kubernetesMetadata = new()
        {
            Config = options.Value.KubernetesConfig ?? new(),
            PortMappingType = options.Value.PortMappingType,
            PublicEntry = options.Value.PublicEntry
        };

        if (!File.Exists(_kubernetesMetadata.Config.KubeConfig))
        {
            logger.SystemLog(StaticLocalizer[nameof(Resources.Program.ContainerProvider_KubernetesConfigLoadFailed),
                _kubernetesMetadata.Config.KubeConfig]);
            throw new FileNotFoundException(_kubernetesMetadata.Config.KubeConfig);
        }

        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(_kubernetesMetadata.Config.KubeConfig);

        _kubernetesMetadata.HostIp = new Uri(config.Host).Host;

        _kubernetesClient = new Kubernetes(config);

        RegistryConfig registryValue = registry.Value;
        var withAuth = !string.IsNullOrWhiteSpace(registryValue.ServerAddress)
                       && !string.IsNullOrWhiteSpace(registryValue.UserName)
                       && !string.IsNullOrWhiteSpace(registryValue.Password);

        if (withAuth)
        {
            var padding =
                $"{registryValue.UserName}@{registryValue.Password}@{registryValue.ServerAddress}".ToMD5String();
            _kubernetesMetadata.AuthSecretName = $"{registryValue.UserName}-{padding}".ToValidRFC1123String("secret");
        }

        try
        {
            InitKubernetes(withAuth, registryValue);
        }
        catch (Exception e)
        {
            logger.LogError(e, "{msg}",
                StaticLocalizer[nameof(Resources.Program.ContainerProvider_KubernetesInitFailed), config.Host]);
            ExitWithFatalMessage(
                StaticLocalizer[nameof(Resources.Program.ContainerProvider_KubernetesInitFailed), config.Host]);
        }

        logger.SystemLog(StaticLocalizer[nameof(Resources.Program.ContainerProvider_KubernetesInited), config.Host],
            TaskStatus.Success,
            LogLevel.Debug);
    }

    public Kubernetes GetProvider() => _kubernetesClient;

    public KubernetesMetadata GetMetadata() => _kubernetesMetadata;

    void InitKubernetes(bool withAuth, RegistryConfig? registry)
    {
        if (_kubernetesClient.CoreV1.ListNamespace().Items
            .All(ns => ns.Metadata.Name != _kubernetesMetadata.Config.Namespace))
            _kubernetesClient.CoreV1.CreateNamespace(
                new() { Metadata = new() { Name = _kubernetesMetadata.Config.Namespace } });

        // skip if policy exists, which can be configured by admin outside GZCTF
        if (_kubernetesClient.NetworkingV1.ListNamespacedNetworkPolicy(_kubernetesMetadata.Config.Namespace).Items
            .All(np => np.Metadata.Name != NetworkPolicy))
            _kubernetesClient.NetworkingV1.CreateNamespacedNetworkPolicy(new()
            {
                Metadata = new() { Name = NetworkPolicy },
                Spec = new()
                {
                    PodSelector = new(),
                    PolicyTypes = ["Egress"],
                    Egress =
                    [
                        new V1NetworkPolicyEgressRule
                        {
                            To =
                            [
                                new V1NetworkPolicyPeer
                                {
                                    IpBlock = new()
                                    {
                                        Cidr = "0.0.0.0/0",
                                        Except = _kubernetesMetadata.Config.AllowCidr ?? ["10.0.0.0/8"]
                                    }
                                }
                            ]
                        }
                    ]
                }
            }, _kubernetesMetadata.Config.Namespace);

        if (!withAuth || registry?.ServerAddress is null)
            return;

        var auth = Codec.Base64.Encode($"{registry.UserName}:{registry.Password}");
        var dockerJsonObj = new
        {
            auths = new Dictionary<string, object>
            {
                {
                    registry.ServerAddress, new { auth, username = registry.UserName, password = registry.Password }
                }
            }
        };

        var dockerJsonBytes = JsonSerializer.SerializeToUtf8Bytes(dockerJsonObj);
        var secret = new V1Secret
        {
            Metadata =
                new V1ObjectMeta
                {
                    Name = _kubernetesMetadata.AuthSecretName,
                    NamespaceProperty = _kubernetesMetadata.Config.Namespace
                },
            Data = new Dictionary<string, byte[]> { [".dockerconfigjson"] = dockerJsonBytes },
            Type = "kubernetes.io/dockerconfigjson"
        };

        try
        {
            _kubernetesClient.CoreV1.ReplaceNamespacedSecret(secret, _kubernetesMetadata.AuthSecretName,
                _kubernetesMetadata.Config.Namespace);
        }
        catch
        {
            _kubernetesClient.CoreV1.CreateNamespacedSecret(secret, _kubernetesMetadata.Config.Namespace);
        }
    }
}
