using Docker.DotNet;
using Docker.DotNet.BasicAuth;
using Docker.DotNet.Models;
using GZCTF.Models.Internal;
using Microsoft.Extensions.Options;

namespace GZCTF.Services.Container.Provider;

public class DockerMetadata : ContainerProviderMetadata
{
    /// <summary>
    /// Docker Configuration
    /// </summary>
    public DockerConfig Config { get; set; } = new();

    /// <summary>
    /// Docker Registry Authentication Configurations
    /// </summary>
    public RegistrySet<AuthConfig> AuthConfigs { get; set; } = new();

    /// <summary>
    /// Generate a unique container name based on the container configuration
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public static string GetName(GZCTF.Models.Internal.ContainerConfig config) =>
        $"{config.Image.Split("/").LastOrDefault()?.Split(":").FirstOrDefault()}_" +
        (config.Flag ?? Guid.NewGuid().ToString("N")).ToMD5String()[..16];
}

public class DockerProvider : IContainerProvider<DockerClient, DockerMetadata>
{
    readonly DockerClient _dockerClient;
    readonly DockerMetadata _dockerMeta;

    public DockerProvider(IOptions<ContainerProvider> options, IOptions<RegistrySet<RegistryConfig>> registriesOptions,
        ILogger<DockerProvider> logger)
    {
        _dockerMeta = new()
        {
            Config = options.Value.DockerConfig ?? new(),
            PortMappingType = options.Value.PortMappingType,
            PublicEntry = options.Value.PublicEntry
        };

        Credentials? credentials = null;

        if (!string.IsNullOrEmpty(_dockerMeta.Config.UserName) && !string.IsNullOrEmpty(_dockerMeta.Config.Password))
            credentials = new BasicAuthCredentials(_dockerMeta.Config.UserName, _dockerMeta.Config.Password);

        DockerClientConfiguration cfg = string.IsNullOrEmpty(_dockerMeta.Config.Uri)
            ? new(credentials)
            : new(new Uri(_dockerMeta.Config.Uri), credentials);

        _dockerClient = cfg.CreateClient();

        var registries = registriesOptions.Value;

        foreach (var registry in registries.Where(registry =>
                     registry.Value.Valid))
        {
            var authConfig = new AuthConfig { Username = registry.Value.UserName, Password = registry.Value.Password };

            if (!_dockerMeta.AuthConfigs.TryAdd(registry.Key, authConfig))
                _dockerMeta.AuthConfigs[registry.Key] = authConfig;
        }

        logger.SystemLog(
            StaticLocalizer[nameof(Resources.Program.ContainerProvider_DockerInited),
                string.IsNullOrEmpty(_dockerMeta.Config.Uri) ? "localhost" : _dockerMeta.Config.Uri],
            TaskStatus.Success, LogLevel.Debug);
    }

    public DockerMetadata GetMetadata() => _dockerMeta;

    public DockerClient GetProvider() => _dockerClient;
}
