using System.Net.Http.Headers;
using GZCTF.Models.Internal;
using GZCTF.Services.Container.ThirdParty;
using Microsoft.Extensions.Options;

namespace GZCTF.Services.Container.Provider;

public class ThirdPartyMetadata : ContainerProviderMetadata
{
    public ThirdPartyConfig Config { get; set; } = new();
}

public class ThirdPartyProvider : IContainerProvider<IThirdPartyClient, ThirdPartyMetadata>
{
    private readonly HttpClient _client;
    private readonly IThirdPartyClient _thirdPartyClient;
    private readonly ThirdPartyMetadata _meta;

    public ThirdPartyProvider(IOptions<ContainerProvider> options, ILogger<ThirdPartyProvider> logger)
    {
        var config = options.Value.ThirdPartyConfig ?? new();

        if (string.IsNullOrWhiteSpace(config.BaseUrl))
        {
            logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.ContainerProvider_ThirdPartyBaseUrlNotConfigured)],
                TaskStatus.Failed, LogLevel.Error);
            throw new InvalidOperationException(
                StaticLocalizer[nameof(Resources.Program.ContainerProvider_ThirdPartyBaseUrlNotConfigured)]);
        }

        _meta = new ThirdPartyMetadata
        {
            Config = config,
            PortMappingType = options.Value.PortMappingType,
            PublicEntry = options.Value.PublicEntry
        };

        var handler = new HttpClientHandler();
        if (!_meta.Config.VerifyTls)
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

        _client = new HttpClient(handler)
        {
            BaseAddress = new Uri(_meta.Config.BaseUrl),
            Timeout = TimeSpan.FromSeconds(Math.Max(1, _meta.Config.Timeout))
        };

        if (!string.IsNullOrWhiteSpace(_meta.Config.ApiToken))
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _meta.Config.ApiToken);

        var apiVersion = _meta.Config.ApiVersion;

        try
        {
            _thirdPartyClient = apiVersion switch
            {
                ThirdPartyApiVersion.V1 => new ThirdPartyClient<ThirdPartyV1.Protocol>(_client, _meta),
                _ => new ThirdPartyClient<ThirdPartyV1.Protocol>(_client, _meta)
            };
        }
        catch (InvalidOperationException ex)
        {
            logger.SystemLog(ex.Message, TaskStatus.Failed, LogLevel.Error);
            throw;
        }

        logger.SystemLog(
            StaticLocalizer[nameof(Resources.Program.ContainerProvider_ThirdPartyInited), apiVersion],
            TaskStatus.Success, LogLevel.Debug);
    }

    public IThirdPartyClient GetProvider() => _thirdPartyClient;

    public ThirdPartyMetadata GetMetadata() => _meta;
}
