using Docker.DotNet;
using Docker.DotNet.BasicAuth;
using Docker.DotNet.Models;
using GZCTF.Models.Internal;
using Microsoft.Extensions.Options;

namespace GZCTF.Services.Container.Provider;

public class DockerMetadata : ContainerProviderMetadata
{
    /// <summary>
    /// Docker 配置
    /// </summary>
    public DockerConfig Config { get; set; } = new();

    /// <summary>
    /// Docker 鉴权用配置
    /// </summary>
    public AuthConfig? Auth { get; set; }

    /// <summary>
    /// 根据配置获取容器名称
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public static string GetName(ContainerConfig config) =>
        $"{config.Image.Split("/").LastOrDefault()?.Split(":").FirstOrDefault()}_" +
        (config.Flag ?? Ulid.NewUlid().ToString().ToLowerInvariant()).ToMD5String()[..16];
}

public class DockerProvider : IContainerProvider<DockerClient, DockerMetadata>
{
    readonly DockerClient _dockerClient;
    readonly DockerMetadata _dockerMeta;

    public DockerProvider(IOptions<ContainerProvider> options, IOptions<RegistryConfig> registry,
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

        // Auth for registry
        if (!string.IsNullOrWhiteSpace(registry.Value.UserName) && !string.IsNullOrWhiteSpace(registry.Value.Password))
            _dockerMeta.Auth =
                new AuthConfig { Username = registry.Value.UserName, Password = registry.Value.Password };

        logger.SystemLog(
            Program.StaticLocalizer[nameof(Resources.Program.ContainerProvider_DockerInited),
                string.IsNullOrEmpty(_dockerMeta.Config.Uri) ? "localhost" : _dockerMeta.Config.Uri],
            TaskStatus.Success, LogLevel.Debug);
    }

    public DockerMetadata GetMetadata() => _dockerMeta;

    public DockerClient GetProvider() => _dockerClient;
}
