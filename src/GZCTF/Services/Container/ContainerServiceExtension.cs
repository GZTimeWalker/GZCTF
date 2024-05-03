using Docker.DotNet;
using GZCTF.Models.Internal;
using GZCTF.Services.Container.Manager;
using GZCTF.Services.Container.Provider;
using GZCTF.Services.Interface;
using k8s;

namespace GZCTF.Services.Container;

public class ContainerProviderMetadata
{
    /// <summary>
    /// 公共访问入口
    /// </summary>
    public string PublicEntry { get; set; } = string.Empty;

    /// <summary>
    /// 端口映射类型
    /// </summary>
    public ContainerPortMappingType PortMappingType { get; set; } = ContainerPortMappingType.Default;

    /// <summary>
    /// 是否直接暴露端口
    /// </summary>
    public bool ExposePort => PortMappingType == ContainerPortMappingType.Default;
}

public static class ContainerServiceExtension
{
    internal static IServiceCollection AddContainerService(this IServiceCollection services,
        IConfiguration configuration)
    {
        ContainerProvider config = configuration.GetSection(nameof(ContainerProvider)).Get<ContainerProvider>() ??
                                   new();

        // FIXME: custom IPortMapper
        return services.AddProvider(config).AddManager(config);
    }

    static IServiceCollection AddProvider(this IServiceCollection services, ContainerProvider config) =>
        config.Type switch
        {
            ContainerProviderType.Docker => services
                .AddSingleton<IContainerProvider<DockerClient, DockerMetadata>, DockerProvider>(),
            ContainerProviderType.Kubernetes => services
                .AddSingleton<IContainerProvider<Kubernetes, KubernetesMetadata>, KubernetesProvider>(),
            _ => services
        };

    static IServiceCollection AddManager(this IServiceCollection services, ContainerProvider config)
    {
        if (config.Type == ContainerProviderType.Kubernetes)
            return services.AddSingleton<IContainerManager, KubernetesManager>();

        if (config.DockerConfig?.SwarmMode is true)
            return services.AddSingleton<IContainerManager, SwarmManager>();

        return services.AddSingleton<IContainerManager, DockerManager>();
    }
}
