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
    /// 
    /// </summary>
    public Dictionary<NetworkMode, string> NetworkNames { get; set; } = new();

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

    string NetworkName(NetworkMode mode) =>
        $"{_dockerMeta.Config.ChallengeNetwork ?? "gzctf"}-{mode.ToString().ToLowerInvariant()}";

    public DockerProvider(IOptions<ContainerProvider> options, IOptions<RegistrySet<RegistryConfig>> registriesOptions,
        ILogger<DockerProvider> logger)
    {
        _dockerMeta = new()
        {
            Config = options.Value.DockerConfig ?? new(),
            PortMappingType = options.Value.PortMappingType,
            NetworkNames = Enum.GetValues<NetworkMode>().ToDictionary(n => n, NetworkName),
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

        EnsureNetworkCreated();

        logger.SystemLog(
            StaticLocalizer[nameof(Resources.Program.ContainerProvider_DockerInited),
                string.IsNullOrEmpty(_dockerMeta.Config.Uri) ? "localhost" : _dockerMeta.Config.Uri],
            TaskStatus.Success, LogLevel.Debug);
    }

    void EnsureNetworkCreated()
    {
        // create two network and attach self container to them (if in container)
        var networks = _dockerClient.Networks.ListNetworksAsync(new NetworksListParameters
        {
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                ["name"] = _dockerMeta.NetworkNames.Values.ToDictionary(name => name, _ => true)
            }
        }).GetAwaiter().GetResult();

        EnsureOpenNetwork(networks);
        EnsureIsolatedNetwork(networks);
    }

    void EnsureOpenNetwork(IList<NetworkResponse> networks)
    {
        var openNetworkName = _dockerMeta.NetworkNames[NetworkMode.Open];
        if (networks.Any(n => n.Name == openNetworkName))
            return;

        _dockerClient.Networks.CreateNetworkAsync(new NetworksCreateParameters
        {
            Name = openNetworkName,
            Driver = "bridge",
            CheckDuplicate = true,
            Attachable = true
        }).GetAwaiter().GetResult();
    }

    void EnsureIsolatedNetwork(IList<NetworkResponse> networks)
    {
        var isolatedNetworkName = _dockerMeta.NetworkNames[NetworkMode.Isolated];
        if (networks.Any(n => n.Name == isolatedNetworkName))
            return;

        _dockerClient.Networks.CreateNetworkAsync(new NetworksCreateParameters
        {
            Name = isolatedNetworkName,
            Driver = "bridge",
            CheckDuplicate = true,
            Attachable = true,
            Internal = true
        }).GetAwaiter().GetResult();
    }

    public DockerMetadata GetMetadata() => _dockerMeta;

    public DockerClient GetProvider() => _dockerClient;
}
