using GZCTF.Models.Internal;
using GZCTF.Models.Request.Info;
using Microsoft.Extensions.Options;

namespace GZCTF.Extensions;

public interface ICaptchaExtension
{
    /// <summary>
    /// 异步校验 token
    /// </summary>
    /// <param name="model">客户端请求</param>
    /// <param name="context">HttpContext</param>
    /// <param name="token"></param>
    /// <returns>校验结果</returns>
    Task<bool> VerifyAsync(ModelWithCaptcha model, HttpContext context, CancellationToken token = default);

    /// <summary>
    /// 获取客户端配置
    /// </summary>
    /// <returns>客户端配置</returns>
    ClientCaptchaInfoModel ClientInfo();
}

public class ModelWithCaptcha
{
    /// <summary>
    /// Captcha Challenge
    /// </summary>
    public string? Challenge { get; set; }
}

public class CaptchaExtensionBase(IOptions<CaptchaConfig>? options) : ICaptchaExtension
{
    protected readonly CaptchaConfig? _config = options?.Value;

    public ClientCaptchaInfoModel ClientInfo() => new(_config);

    public virtual Task<bool> VerifyAsync(ModelWithCaptcha model, HttpContext context, CancellationToken token = default)
        => Task.FromResult(true);
}

public sealed class GoogleRecaptchaExtension(IOptions<CaptchaConfig>? options) : CaptchaExtensionBase(options)
{
    private readonly HttpClient _httpClient = new();

    public override async Task<bool> VerifyAsync(ModelWithCaptcha model, HttpContext context, CancellationToken token = default)
    {
        if (_config is null || string.IsNullOrWhiteSpace(_config.SecretKey))
            return true;

        if (string.IsNullOrEmpty(model.Challenge) || context.Connection.RemoteIpAddress is null)
            return false;

        var ip = context.Connection.RemoteIpAddress;
        var api = _config.GoogleRecaptcha.VerifyAPIAddress;

        var result = await _httpClient.GetAsync($"{api}?secret={_config.SecretKey}&response={token}&remoteip={ip}", token);
        var res = await result.Content.ReadFromJsonAsync<RecaptchaResponseModel>(cancellationToken: token);

        return res is not null && res.Success && res.Score >= _config.GoogleRecaptcha.RecaptchaThreshold;
    }
}

public sealed class CloudflareTurnstile(IOptions<CaptchaConfig>? options) : CaptchaExtensionBase(options)
{
    private readonly HttpClient _httpClient = new();

    public override async Task<bool> VerifyAsync(ModelWithCaptcha model, HttpContext context, CancellationToken token = default)
    {
        if (_config is null || string.IsNullOrWhiteSpace(_config.SecretKey))
            return true;

        if (string.IsNullOrEmpty(model.Challenge) || context.Connection.RemoteIpAddress is null)
            return false;

        var ip = context.Connection.RemoteIpAddress;

        TurnstileRequestModel req = new()
        {
            Secret = _config.SecretKey,
            Response = model.Challenge,
            RemoteIP = ip.ToString()
        };

        const string api = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

        var result = await _httpClient.PostAsJsonAsync(api, req, token);
        var res = await result.Content.ReadFromJsonAsync<TurnstileResponseModel>(cancellationToken: token);

        return res is not null && res.Success;
    }
}

public static class CaptchaServiceExtension
{
    internal static IServiceCollection AddCaptchaService(this IServiceCollection services, ConfigurationManager configuration)
    {
        var config = configuration.GetSection(nameof(CaptchaConfig)).Get<CaptchaConfig>() ?? new();

        services.Configure<CaptchaConfig>(configuration.GetSection(nameof(CaptchaConfig)));

        return config.Provider switch
        {
            CaptchaProvider.GoogleRecaptcha => services.AddSingleton<ICaptchaExtension, GoogleRecaptchaExtension>(),
            CaptchaProvider.CloudflareTurnstile => services.AddSingleton<ICaptchaExtension, CloudflareTurnstile>(),
            _ => services.AddSingleton<ICaptchaExtension, CaptchaExtensionBase>(),
        };
    }
}
