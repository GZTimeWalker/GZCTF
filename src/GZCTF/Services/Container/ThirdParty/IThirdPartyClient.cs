using GZCTF.Models.Internal;

namespace GZCTF.Services.Container.ThirdParty;

/// <summary>
/// Provides a normalized client for third-party container APIs.
/// </summary>
public interface IThirdPartyClient
{
    /// <summary>
    /// Gets the API version in use.
    /// </summary>
    ThirdPartyApiVersion ApiVersion { get; }

    /// <summary>
    /// Creates a container through the third-party API.
    /// </summary>
    Task<IThirdPartyCreateResponse?> CreateAsync(ContainerConfig config, string requestId,
        CancellationToken token = default);

    /// <summary>
    /// Destroys a container by id.
    /// </summary>
    Task<bool> DestroyAsync(string containerId, CancellationToken token = default);
}
