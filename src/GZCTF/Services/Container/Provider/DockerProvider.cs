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
    /// Network names for different modes
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
    private readonly DockerClient _dockerClient;
    private readonly DockerMetadata _dockerMeta;

    public DockerProvider(IOptions<ContainerProvider> options, IOptions<RegistrySet<RegistryConfig>> registriesOptions,
        ILogger<DockerProvider> logger)
    {
        var config = options.Value.DockerConfig ?? new();

        _dockerMeta = new()
        {
            Config = config,
            PortMappingType = options.Value.PortMappingType,
            NetworkNames =
                Enum.GetValues<NetworkMode>()
                    .ToDictionary(n => n,
                        m => $"{config.ChallengeNetwork ?? "gzctf"}-{m.ToString().ToLowerInvariant()}"),
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

    private void EnsureNetworkCreated()
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

        // always try to attach to custom network if exists
        var customNetworkName = _dockerMeta.NetworkNames[NetworkMode.Custom];
        if (networks.Any(n => n.Name == customNetworkName))
            AttachSelfToNetwork(customNetworkName);
    }

    private void EnsureOpenNetwork(IList<NetworkResponse> networks)
    {
        var openNetworkName = _dockerMeta.NetworkNames[NetworkMode.Open];
        if (networks.All(n => n.Name != openNetworkName))
        {
            _dockerClient.Networks.CreateNetworkAsync(new NetworksCreateParameters
            {
                Name = openNetworkName,
                Driver = "bridge",
                Attachable = true
            }).GetAwaiter().GetResult();
        }

        AttachSelfToNetwork(openNetworkName);
    }

    private void EnsureIsolatedNetwork(IList<NetworkResponse> networks)
    {
        var isolatedNetworkName = _dockerMeta.NetworkNames[NetworkMode.Isolated];
        if (networks.All(n => n.Name != isolatedNetworkName))
        {
            // Do not use internal network, it will disable the port mapping feature of docker
            // reference: https://github.com/moby/moby/issues/36174#issuecomment-2527195596
            _dockerClient.Networks.CreateNetworkAsync(new NetworksCreateParameters
            {
                Name = isolatedNetworkName,
                Driver = "bridge",
                Attachable = true,
                Options = new Dictionary<string, string>
                {
                    ["com.docker.network.bridge.enable_ip_masquerade"] = "false"
                }
            }).GetAwaiter().GetResult();
        }

        AttachSelfToNetwork(isolatedNetworkName);
    }

    private void AttachSelfToNetwork(string networkName)
    {
        var selfContainerId = Environment.GetEnvironmentVariable("HOSTNAME");
        if (string.IsNullOrEmpty(selfContainerId))
            return;

        try
        {
            _dockerClient.Networks.ConnectNetworkAsync(networkName,
                new NetworkConnectParameters { Container = selfContainerId }).GetAwaiter().GetResult();
        }
        catch
        {
            // ignore errors
        }
    }

    public DockerMetadata GetMetadata() => _dockerMeta;

    public DockerClient GetProvider() => _dockerClient;
}
