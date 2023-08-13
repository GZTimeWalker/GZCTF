﻿using System.Text.Json;
using GZCTF.Models.Internal;
using GZCTF.Services.Interface;
using GZCTF.Utils;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Options;

namespace GZCTF.Services;

public class K8sMetadata
{
    /// <summary>
    /// 容器注册表鉴权 Secret 名称
    /// </summary>
    public string? AuthSecretName { get; set; }

    /// <summary>
    /// K8s 集群 Host IP
    /// </summary>
    public string HostIP { get; set; } = string.Empty;

    /// <summary>
    /// 公共访问入口
    /// </summary>
    public string PublicEntry { get; set; } = string.Empty;

    /// <summary>
    /// K8s 配置
    /// </summary>
    public K8sConfig Config { get; set; } = new();
}

public class K8sProvider : IContainerProvider<Kubernetes, K8sMetadata>
{
    private const string NetworkPolicy = "gzctf-policy";

    private readonly Kubernetes _kubernetesClient;
    private readonly K8sMetadata _k8sMetadata;

    public Kubernetes GetProvider() => _kubernetesClient;
    public K8sMetadata GetMetadata() => _k8sMetadata;

    public K8sProvider(IOptions<RegistryConfig> registry, IOptions<ContainerProvider> provider, ILogger<K8sProvider> logger)
    {
        _k8sMetadata = new()
        {
            Config = provider.Value.K8sConfig ?? new(),
            PublicEntry = provider.Value.PublicEntry
        };

        if (!File.Exists(_k8sMetadata.Config.KubeConfig))
        {
            LogHelper.SystemLog(logger, $"无法加载 K8s 配置文件，请确保配置文件存在 {_k8sMetadata.Config.KubeConfig}");
            throw new FileNotFoundException(_k8sMetadata.Config.KubeConfig);
        }

        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(_k8sMetadata.Config.KubeConfig);

        _k8sMetadata.HostIP = config.Host[(config.Host.LastIndexOf('/') + 1)..config.Host.LastIndexOf(':')];

        _kubernetesClient = new Kubernetes(config);

        var registryValue = registry.Value;
        var withAuth = !string.IsNullOrWhiteSpace(registryValue.ServerAddress)
            && !string.IsNullOrWhiteSpace(registryValue.UserName)
            && !string.IsNullOrWhiteSpace(registryValue.Password);

        if (withAuth)
        {
            var padding = $"{registryValue.UserName}@{registryValue.Password}@{registryValue.ServerAddress}".StrMD5();
            _k8sMetadata.AuthSecretName = $"{registryValue.UserName}-{padding}".ToValidRFC1123String("secret");
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

        logger.SystemLog($"K8s 初始化成功 ({config.Host})", TaskStatus.Success, LogLevel.Debug);
    }

    private void InitK8s(bool withAuth, RegistryConfig? registry)
    {
        if (_kubernetesClient.CoreV1.ListNamespace().Items.All(ns => ns.Metadata.Name != _k8sMetadata.Config.Namespace))
            _kubernetesClient.CoreV1.CreateNamespace(new() { Metadata = new() { Name = _k8sMetadata.Config.Namespace } });

        if (_kubernetesClient.NetworkingV1.ListNamespacedNetworkPolicy(_k8sMetadata.Config.Namespace).Items.All(np => np.Metadata.Name != NetworkPolicy))
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
                                        Except = _k8sMetadata.Config.AllowCIDR ?? new[] { "10.0.0.0/8" }
                                    }
                                },
                            }
                        }
                    }
                }
            }, _k8sMetadata.Config.Namespace);
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
                    Name = _k8sMetadata.AuthSecretName,
                    NamespaceProperty = _k8sMetadata.Config.Namespace,
                },
                Data = new Dictionary<string, byte[]>() { [".dockerconfigjson"] = dockerjsonBytes },
                Type = "kubernetes.io/dockerconfigjson"
            };

            try
            {
                _kubernetesClient.CoreV1.ReplaceNamespacedSecret(secret, _k8sMetadata.AuthSecretName, _k8sMetadata.Config.Namespace);
            }
            catch
            {
                _kubernetesClient.CoreV1.CreateNamespacedSecret(secret, _k8sMetadata.Config.Namespace);
            }
        }
    }
}
