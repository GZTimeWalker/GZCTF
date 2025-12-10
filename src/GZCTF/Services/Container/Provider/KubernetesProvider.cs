using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using GZCTF.Models.Internal;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Options;

namespace GZCTF.Services.Container.Provider;

public class KubernetesMetadata : ContainerProviderMetadata
{
    /// <summary>
    /// The secret names for registry authentication
    /// </summary>
    public RegistrySet<string> AuthSecretNames { get; set; } = new();

    /// <summary>
    /// Host IP address
    /// </summary>
    public string HostIp { get; set; } = string.Empty;

    /// <summary>
    /// Kubernetes Configuration
    /// </summary>
    public KubernetesConfig Config { get; set; } = new();
}

public class KubernetesProvider : IContainerProvider<Kubernetes, KubernetesMetadata>
{
    private readonly Kubernetes _kubernetesClient;
    private readonly KubernetesMetadata _kubernetesMetadata;

    public KubernetesProvider(IOptions<RegistrySet<RegistryConfig>> registries, IOptions<ContainerProvider> options,
        ILogger<KubernetesProvider> logger)
    {
        _kubernetesMetadata = new()
        {
            Config = options.Value.KubernetesConfig ?? new(),
            PortMappingType = options.Value.PortMappingType,
            PublicEntry = options.Value.PublicEntry
        };

        KubernetesClientConfiguration config;

        if (!string.IsNullOrWhiteSpace(_kubernetesMetadata.Config.KubeConfig) &&
            File.Exists(_kubernetesMetadata.Config.KubeConfig))
        {
            config = KubernetesClientConfiguration.BuildConfigFromConfigFile(_kubernetesMetadata.Config.KubeConfig);
        }
        else if (KubernetesClientConfiguration.IsInCluster())
        {
            // use ServiceAccount token if running in cluster and no kube-config is provided
            config = KubernetesClientConfiguration.InClusterConfig();
        }
        else
        {
            logger.SystemLog(StaticLocalizer[nameof(Resources.Program.ContainerProvider_KubernetesConfigLoadFailed),
                _kubernetesMetadata.Config.KubeConfig]);
            throw new FileNotFoundException(_kubernetesMetadata.Config.KubeConfig);
        }

        _kubernetesMetadata.HostIp = new Uri(config.Host).Host;
        _kubernetesClient = new Kubernetes(config);

        try
        {
            InitKubernetes(registries.Value);
        }
        catch (Exception e)
        {
            logger.LogErrorMessage(e,
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

    private void InitKubernetes(RegistrySet<RegistryConfig> registries)
    {
        if (_kubernetesClient.CoreV1.ListNamespace().Items
            .All(ns => ns.Metadata.Name != _kubernetesMetadata.Config.Namespace))
            _kubernetesClient.CoreV1.CreateNamespace(
                new() { Metadata = new() { Name = _kubernetesMetadata.Config.Namespace } });

        // create network policies (replace if exists)
        EnsureNetworkPolicy(OpenNetworkPolicy);
        EnsureNetworkPolicy(IsolatedNetworkPolicy);

        // create auth secrets for registries
        foreach (var registry in registries.Where(registry => registry.Value.Valid))
            InsertRegistrySecret(registry.Key, registry.Value);
    }

    private void EnsureNetworkPolicy(V1NetworkPolicy policy)
    {
        var policyName = policy.Metadata.Name;
        try
        {
            _kubernetesClient.NetworkingV1.ReplaceNamespacedNetworkPolicy(policy, policyName,
                _kubernetesMetadata.Config.Namespace);
        }
        catch
        {
            _kubernetesClient.NetworkingV1.CreateNamespacedNetworkPolicy(policy, _kubernetesMetadata.Config.Namespace);
        }
    }

    private const string IsolatedNetworkPolicyName = "gzctf-network-isolated";
    private const string OpenNetworkPolicyName = "gzctf-network-open";

    /// <summary>
    /// Isolated Network Policy
    /// </summary>
    /// <remarks>
    ///  Blocks all outbound traffic.
    /// </remarks>
    private static readonly V1NetworkPolicy IsolatedNetworkPolicy = new()
    {
        Metadata = new V1ObjectMeta { Name = IsolatedNetworkPolicyName, },
        Spec = new V1NetworkPolicySpec
        {
            PodSelector = new V1LabelSelector
            {
                MatchLabels = new Dictionary<string, string>
                {
                    ["gzctf.gzti.me/NetworkMode"] = nameof(NetworkMode.Isolated).ToLowerInvariant()
                }
            },
            PolicyTypes = ["Egress"],
            Egress = []
        }
    };

    /// <summary>
    ///  Open Network Policy
    /// </summary>
    /// <remarks>
    ///  Allows all outbound traffic except to the specified CIDR blocks in the Kubernetes configuration.
    /// </remarks>
    private V1NetworkPolicy OpenNetworkPolicy =>
        new()
        {
            Metadata = new() { Name = OpenNetworkPolicyName },
            Spec = new()
            {
                PodSelector = new V1LabelSelector
                {
                    MatchLabels = new Dictionary<string, string>
                    {
                        ["gzctf.gzti.me/NetworkMode"] = nameof(NetworkMode.Open).ToLowerInvariant()
                    }
                },
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
        };

    private void InsertRegistrySecret(string address, RegistryConfig registry)
    {
        var padding = $"GZCTF@{registry.UserName}@{address}".ToMD5String();
        var secretName = $"{registry.UserName}-{padding}".ToValidRFC1123String("secret");

        var auth = Codec.Base64.Encode($"{registry.UserName}:{registry.Password}");
        var dockerJsonObj = new DockerRegistryOptions(
            new Dictionary<string, DockerRegistryEntry> { [address] = new(auth, registry.UserName, registry.Password) }
        );

        var dockerJsonBytes =
            JsonSerializer.SerializeToUtf8Bytes(dockerJsonObj, AppJsonSerializerContext.Default.DockerRegistryOptions);
        var secret = new V1Secret
        {
            Metadata =
                new V1ObjectMeta { Name = secretName, NamespaceProperty = _kubernetesMetadata.Config.Namespace },
            Data = new Dictionary<string, byte[]> { [".dockerconfigjson"] = dockerJsonBytes },
            Type = "kubernetes.io/dockerconfigjson"
        };

        try
        {
            _kubernetesClient.CoreV1.ReplaceNamespacedSecret(secret, secretName,
                _kubernetesMetadata.Config.Namespace);
        }
        catch
        {
            _kubernetesClient.CoreV1.CreateNamespacedSecret(secret, _kubernetesMetadata.Config.Namespace);
        }

        if (!_kubernetesMetadata.AuthSecretNames.TryAdd(address, secretName))
            _kubernetesMetadata.AuthSecretNames[address] = secretName;
    }
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal record DockerRegistryOptions(Dictionary<string, DockerRegistryEntry> auths);

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal record DockerRegistryEntry(string auth, string? username, string? password);
