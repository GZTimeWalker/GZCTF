using System.Security.Cryptography;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Info;
using GZCTF.Services.Cache;
using Microsoft.Extensions.Caching.Distributed;
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

        var ip = context.Connection.RemoteIpAddress;

        TurnstileRequestModel req = new()
        {
            Secret = Config.SecretKey,
            Response = model.Challenge,
            RemoteIp = ip.ToString()
        };

        const string api = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

        var result = await _httpClient.PostAsJsonAsync(api, req, token);
        var res = await result.Content.ReadFromJsonAsync<TurnstileResponseModel>(token);

        return res is not null && res.Success;
    }
}

public sealed class HashPow(IOptions<CaptchaConfig>? options, IDistributedCache cache) :
    CaptchaExtensionBase(options)
{
    const int AnswerLength = 8;

    public override async Task<bool> VerifyAsync(ModelWithCaptcha model, HttpContext context,
        CancellationToken token = default)
    {
        if (Config is null)
            return true;

        if (string.IsNullOrWhiteSpace(model.Challenge))
            return false;

        var parts = model.Challenge.Split(':');
        if (parts.Length != 2)
            return false;

        var id = parts[0];
        var ans = parts[1];
        if (ans.Length != AnswerLength * 2)
            return false;

        var key = CacheKey.HashPow(id);
        var challenge = await cache.GetAsync(key, token);
        if (challenge is null)
            return false;

        Span<byte> span = stackalloc byte[challenge.Length + AnswerLength];
        challenge.CopyTo(span);
        Convert.FromHexString(ans).CopyTo(span[challenge.Length..]);

        var leadingZeros = SHA256.HashData(span).LeadingZeros();

        var result = leadingZeros >= Config.HashPow.Difficulty;
        if (result)
            await cache.RemoveAsync(key, token);

        return result;
    }
}

public static class CaptchaServiceExtension
{
    internal static IServiceCollection AddCaptchaService(this IServiceCollection services,
        IConfiguration configuration)
    {
        var config = configuration.GetSection(nameof(CaptchaConfig)).Get<CaptchaConfig>() ?? new();

        services.Configure<CaptchaConfig>(configuration.GetSection(nameof(CaptchaConfig)));

        return config.Provider switch
        {
            CaptchaProvider.HashPow => services.AddSingleton<ICaptchaExtension, HashPow>(),
            CaptchaProvider.CloudflareTurnstile => services.AddSingleton<ICaptchaExtension, CloudflareTurnstile>(),
            _ => services.AddSingleton<ICaptchaExtension, CaptchaExtensionBase>()
        };
    }
}
