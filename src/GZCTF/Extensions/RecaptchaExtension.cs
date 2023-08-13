using System.Text.Json;
using GZCTF.Models.Internal;
using Microsoft.Extensions.Options;

namespace GZCTF.Extensions;

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

public class RecaptchaExtension : IRecaptchaExtension
{
    private readonly RecaptchaConfig? _options;
    private readonly HttpClient _httpClient;

    public RecaptchaExtension(IOptions<RecaptchaConfig> options)
    {
        _options = options.Value;
        _httpClient = new();
    }

    public async Task<bool> VerifyAsync(string token, string ip)
    {
        if (_options is null)
            return true;

        if (string.IsNullOrEmpty(token))
            return false;

        var response = await _httpClient.GetStreamAsync($"{_options.VerifyAPIAddress}?secret={_options.Secretkey}&response={token}&remoteip={ip}");
        var tokenResponse = await JsonSerializer.DeserializeAsync<TokenResponseModel>(response);
        if (tokenResponse is null || !tokenResponse.Success || tokenResponse.Score < _options.RecaptchaThreshold)
            return false;

        return true;
    }

    public string SiteKey() => _options?.SiteKey ?? "NOTOKEN";
}
