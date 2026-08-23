using System.Text.Json;
using System.Text.Json.Serialization;
using GZCTF.Models.Internal;

namespace GZCTF.Services.Container.ThirdParty;

/// <summary>
/// Defines a third-party API contract for a specific version.
/// </summary>
public interface IThirdPartyProtocol
{
    /// <summary>
    /// Gets the API version implemented by this protocol.
    /// </summary>
    ThirdPartyApiVersion Version { get; }

    /// <summary>
    /// Gets the idempotency request id header name.
    /// </summary>
    string RequestIdHeader { get; }

    /// <summary>
    /// Gets the API path for creating a container.
    /// </summary>
    string CreatePath { get; }

    /// <summary>
    /// Gets the API path for destroying a container.
    /// </summary>
    string DestroyPath { get; }

    /// <summary>
    /// Builds a create request payload for the protocol version.
    /// </summary>
    object BuildCreateRequest(ContainerConfig config);

    /// <summary>
    /// Parses the create response payload to a normalized interface.
    /// </summary>
    IThirdPartyCreateResponse? DeserializeCreateResponse(string json, JsonSerializerOptions options);
}

/// <summary>
/// Normalized create response shape returned by third-party providers.
/// </summary>
public interface IThirdPartyCreateResponse
{
    string? Id { get; }
    ThirdPartyContainerState? State { get; }
    ThirdPartyAddress? InternalAddress { get; }
    ThirdPartyAddress? ExternalAddress { get; }
    DateTimeOffset? StartedAt { get; }
    DateTimeOffset? ExpectStopAt { get; }
}

[JsonConverter(typeof(JsonStringEnumConverter<ThirdPartyContainerState>))]
public enum ThirdPartyContainerState
{
    Unknown,
    Running,
    Failed
}

public sealed class ThirdPartyAddress
{
    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; }
}
