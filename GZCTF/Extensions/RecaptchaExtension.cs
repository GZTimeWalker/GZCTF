using System.Text.Json;
using CTFServer.Models;

namespace CTFServer.Extensions;

public interface IRecaptchaExtension
{
    Task<bool> VerifyAsync(string token, string ip);
}

public class RecaptchaExtension : IRecaptchaExtension
{
    private IConfiguration _configuration { get; }
    private static string GoogleSecretKey { get; set; } = string.Empty;
    private static string GoogleRecaptchaVerifyApi { get; set; } = string.Empty;
    private static decimal RecaptchaThreshold { get; set; } = 1;
    public RecaptchaExtension()
    {
        _configuration = new ConfigurationBuilder()
                            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                            .Build();

        GoogleRecaptchaVerifyApi = _configuration.GetSection("GoogleRecaptcha").GetSection("VefiyAPIAddress").Value ?? "";
        GoogleSecretKey = _configuration.GetSection("GoogleRecaptcha").GetSection("Secretkey").Value ?? "";

        var hasThresholdValue = decimal.TryParse(_configuration.GetSection("RecaptchaThreshold").Value ?? "", out var threshold);
        if (hasThresholdValue)
            RecaptchaThreshold = threshold;
    }
    public async Task<bool> VerifyAsync(string token, string ip)
    {
        if (string.IsNullOrEmpty(token))
            return false;
        using (var client = new HttpClient())
        {
            var response = await client.GetStringAsync($"{GoogleRecaptchaVerifyApi}?secret={GoogleSecretKey}&response={token}&remoteip={ip}");
            var tokenResponse = JsonSerializer.Deserialize<TokenResponseModel>(response);
            if (tokenResponse is null || !tokenResponse.Success || tokenResponse.Score < RecaptchaThreshold)
                return false;
        }
        return true;
    }
}