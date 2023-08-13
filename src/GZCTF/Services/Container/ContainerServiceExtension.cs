using Docker.DotNet;
using GZCTF.Models.Internal;
using GZCTF.Services.Interface;
using k8s;

namespace GZCTF.Services;

public static class ContainerServiceExtension
{
    internal static IServiceCollection AddContainerService(this IServiceCollection services, ConfigurationManager configuration)
    {
        var provider = configuration.GetSection(nameof(ContainerProvider));
        var type = provider.GetValue<ContainerProviderType>(nameof(ContainerProvider.Type));

        // FIXME: custom IPortMapper
        if (type == ContainerProviderType.Kubernetes)
        {
            services.AddSingleton<IContainerProvider<Kubernetes, K8sMetadata>, K8sProvider>();

            services.AddSingleton<IPortMapper, K8sNodePortMapper>();
            services.AddSingleton<IContainerManager, K8sManager>();
        }
        else if (type == ContainerProviderType.Docker)
        {
            services.AddSingleton<IContainerProvider<DockerClient, DockerMetadata>, DockerProvider>();

            var docker = provider.GetValue<DockerConfig?>(nameof(ContainerProvider.DockerConfig));

            if (docker?.SwarmMode is true)
            {
                services.AddSingleton<IPortMapper, SwarmDirectMapper>();
                services.AddSingleton<IContainerManager, SwarmManager>();
            }
            else
            {
                services.AddSingleton<IPortMapper, DockerDirectMapper>();
                services.AddSingleton<IContainerManager, DockerManager>();
            }
        }

        return services;
    }
}
