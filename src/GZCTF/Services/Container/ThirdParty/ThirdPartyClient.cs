using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GZCTF.Models.Internal;
using GZCTF.Services.Container.Provider;

namespace GZCTF.Services.Container.ThirdParty;

public class ThirdPartyClient<TProtocol> : IThirdPartyClient
    where TProtocol : IThirdPartyProtocol, new()
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly HttpClient _client;
    private readonly ThirdPartyMetadata _meta;
    private readonly TProtocol _protocol;

    public ThirdPartyClient(HttpClient client, ThirdPartyMetadata meta)
    {
        _meta = meta;
        _client = client;
        _protocol = new TProtocol();

        if (_meta.Config.ApiVersion != _protocol.Version)
            throw new InvalidOperationException(
                $"Third-party protocol version mismatch: config={_meta.Config.ApiVersion}, " +
                $"protocol={_protocol.Version}.");
    }

    public ThirdPartyApiVersion ApiVersion => _protocol.Version;

    public async Task<IThirdPartyCreateResponse?> CreateAsync(ContainerConfig config, string requestId,
        CancellationToken token = default)
    {
        var request = _protocol.BuildCreateRequest(config);
        var content = JsonSerializer.Serialize(request, JsonOptions);

        using var message = new HttpRequestMessage(HttpMethod.Post, _protocol.CreatePath)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        message.Headers.TryAddWithoutValidation(_protocol.RequestIdHeader, requestId);

        var response = await _client.SendAsync(message, token);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(token);
            throw new ThirdPartyRequestException(response.StatusCode, body);
        }

        var responseBody = await response.Content.ReadAsStringAsync(token);
        return _protocol.DeserializeCreateResponse(responseBody, JsonOptions);
    }

    public async Task<bool> DestroyAsync(string containerId, CancellationToken token = default)
    {
        var path = _protocol.DestroyPath.Replace("{id}", Uri.EscapeDataString(containerId));
        using var message = new HttpRequestMessage(HttpMethod.Delete, path);

        var response = await _client.SendAsync(message, token);
        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
            return true;

        var body = await response.Content.ReadAsStringAsync(token);
        throw new ThirdPartyRequestException(response.StatusCode, body);
    }

}
