using System.Net;
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
    protected readonly CaptchaConfig? Config = options?.Value;

    public ClientCaptchaInfoModel ClientInfo() => new(Config);

    public virtual Task<bool> VerifyAsync(ModelWithCaptcha model, HttpContext context,
        CancellationToken token = default) =>
        Task.FromResult(true);
}

public sealed class GoogleRecaptchaExtension(IOptions<CaptchaConfig>? options) : CaptchaExtensionBase(options)
{
    readonly HttpClient _httpClient = new();

    public override async Task<bool> VerifyAsync(ModelWithCaptcha model, HttpContext context,
        CancellationToken token = default)
    {
        if (Config is null || string.IsNullOrWhiteSpace(Config.SecretKey))
            return true;

        if (string.IsNullOrEmpty(model.Challenge) || context.Connection.RemoteIpAddress is null)
            return false;

        IPAddress? ip = context.Connection.RemoteIpAddress;
        var api = Config.GoogleRecaptcha.VerifyApiAddress;

        HttpResponseMessage result =
            await _httpClient.GetAsync($"{api}?secret={Config.SecretKey}&response={model.Challenge}&remoteip={ip}",
                token);
        var res = await result.Content.ReadFromJsonAsync<RecaptchaResponseModel>(token);

        return res is not null && res.Success && res.Score >= Config.GoogleRecaptcha.RecaptchaThreshold;
    }
}

public sealed class CloudflareTurnstile(IOptions<CaptchaConfig>? options) : CaptchaExtensionBase(options)
{
    readonly HttpClient _httpClient = new();

    public override async Task<bool> VerifyAsync(ModelWithCaptcha model, HttpContext context,
        CancellationToken token = default)
    {
        if (Config is null || string.IsNullOrWhiteSpace(Config.SecretKey))
            return true;

        if (string.IsNullOrEmpty(model.Challenge) || context.Connection.RemoteIpAddress is null)
            return false;

        IPAddress? ip = context.Connection.RemoteIpAddress;

        TurnstileRequestModel req = new()
        {
            Secret = Config.SecretKey,
            Response = model.Challenge,
            RemoteIp = ip.ToString()
        };

        const string api = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

        HttpResponseMessage result = await _httpClient.PostAsJsonAsync(api, req, token);
        var res = await result.Content.ReadFromJsonAsync<TurnstileResponseModel>(token);

        return res is not null && res.Success;
    }
}

public static class CaptchaServiceExtension
{
    internal static IServiceCollection AddCaptchaService(this IServiceCollection services,
        IConfiguration configuration)
    {
        CaptchaConfig config = configuration.GetSection(nameof(CaptchaConfig)).Get<CaptchaConfig>() ?? new();

        services.Configure<CaptchaConfig>(configuration.GetSection(nameof(CaptchaConfig)));

        return config.Provider switch
        {
            CaptchaProvider.GoogleRecaptcha => services.AddSingleton<ICaptchaExtension, GoogleRecaptchaExtension>(),
            CaptchaProvider.CloudflareTurnstile => services.AddSingleton<ICaptchaExtension, CloudflareTurnstile>(),
            _ => services.AddSingleton<ICaptchaExtension, CaptchaExtensionBase>()
        };
    }
}
