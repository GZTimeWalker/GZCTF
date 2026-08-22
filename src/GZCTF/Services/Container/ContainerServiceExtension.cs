using Docker.DotNet;
using GZCTF.Models.Internal;
using GZCTF.Services.Container.Manager;
using GZCTF.Services.Container.Provider;
using k8s;

namespace GZCTF.Services.Container;

public class ContainerProviderMetadata
{
    /// <summary>
    /// The public entry address for accessing the containers
    /// </summary>
    public string? PublicEntry { get; set; }

    /// <summary>
    /// Port mapping type
    /// </summary>
    public ContainerPortMappingType PortMappingType { get; set; } = ContainerPortMappingType.Default;

    /// <summary>
    /// Whether to expose ports directly
    /// </summary>
    public bool ExposePort => PortMappingType == ContainerPortMappingType.Default;
}

public static class ContainerServiceExtension
{
    extension(IServiceCollection services)
    {
        internal IServiceCollection AddContainerService(IConfiguration configuration)
        {
            var config = configuration.GetSection(nameof(ContainerProvider)).Get<ContainerProvider>() ??
                         new();

            // FIXME: custom IPortMapper
            return services.AddProvider(config).AddManager(config);
        }

        private IServiceCollection AddProvider(ContainerProvider config) =>
            config.Type switch
            {
                ContainerProviderType.Docker => services
                    .AddSingleton<IContainerProvider<DockerClient, DockerMetadata>, DockerProvider>(),
                ContainerProviderType.Kubernetes => services
                    .AddSingleton<IContainerProvider<Kubernetes, KubernetesMetadata>, KubernetesProvider>(),
                _ => services
            };

        private IServiceCollection AddManager(ContainerProvider config)
            => config.Type switch
            {
                ContainerProviderType.Kubernetes => services.AddSingleton<IContainerManager, KubernetesManager>(),
                _ => services.AddSingleton<IContainerManager, DockerManager>()
            };
    }
}
