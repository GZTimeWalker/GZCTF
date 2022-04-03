using System.Text.Json;
using CTFServer.Models.Internal;
using Microsoft.Extensions.Options;

namespace CTFServer.Extensions;

public interface IRecaptchaExtension
{
    Task<bool> VerifyAsync(string token, string ip);
}

public record RecaptchaOptions(string Secretkey, string VefiyAPIAddress, float RecaptchaThreshold = 1);

public class RecaptchaExtension : IRecaptchaExtension
{
    private readonly RecaptchaOptions options;

    public RecaptchaExtension(IOptions<RecaptchaOptions> options)
    {
        this.options = options.Value;
    }
    public async Task<bool> VerifyAsync(string token, string ip)
    {
        if (string.IsNullOrEmpty(token))
            return false;
        using (var client = new HttpClient())
        {
            var response = await client.GetStreamAsync($"{options.VefiyAPIAddress}?secret={options.Secretkey}&response={token}&remoteip={ip}");
            var tokenResponse = await JsonSerializer.DeserializeAsync<TokenResponseModel>(response);
            if (tokenResponse is null || !tokenResponse.Success || tokenResponse.Score < options.RecaptchaThreshold)
                return false;
        }
        return true;
    }
}