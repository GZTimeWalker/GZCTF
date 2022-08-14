using CTFServer.Models.Internal;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CTFServer.Extensions;

public interface IRecaptchaExtension
{
    /// <summary>
    /// 异步校验 token
    /// </summary>
    /// <param name="token">google recaptcha token</param>
    /// <param name="ip">user ip</param>
    /// <returns>校验结果</returns>
    Task<bool> VerifyAsync(string token, string ip);

    /// <summary>
    /// 获取 Sitekey
    /// </summary>
    /// <returns>Sitekey</returns>
    string SiteKey();
}

public class RecaptchaOptions
{
    public bool IsEnable { get; set; } = false;
    public string? Secretkey { get; set; }
    public string? SiteKey { get; set; }
    public string VerifyAPIAddress { get; set; } = "https://www.recaptcha.net/recaptcha/api/siteverify";
    public float? RecaptchaThreshold { get; set; } = 0.5f;
}

public class RecaptchaExtension : IRecaptchaExtension
{
    private readonly RecaptchaOptions options;
    private readonly HttpClient httpClient;

    public RecaptchaExtension(IOptions<RecaptchaOptions> options)
    {
        this.options = options.Value;
        httpClient = new();
    }

    public async Task<bool> VerifyAsync(string token, string ip)
    {
        if (!options.IsEnable)
            return true;

        if (string.IsNullOrEmpty(token))
            return false;

        var response = await httpClient.GetStreamAsync($"{options.VerifyAPIAddress}?secret={options.Secretkey}&response={token}&remoteip={ip}");
        var tokenResponse = await JsonSerializer.DeserializeAsync<TokenResponseModel>(response);
        if (tokenResponse is null || !tokenResponse.Success || tokenResponse.Score < options.RecaptchaThreshold)
            return false;

        return true;
    }

    public string SiteKey() => options.IsEnable ? options.SiteKey ?? "NOTOKEN" : "NOTOKEN";
}