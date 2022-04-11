using System.Text.Json;
using CTFServer.Models.Internal;
using Microsoft.Extensions.Options;

namespace CTFServer.Extensions;

public interface IRecaptchaExtension
{
    Task<bool> VerifyAsync(string token, string ip);
}

public class RecaptchaOptions
{
    public string Secretkey { get; set; } = default!;
    public string VefiyAPIAddress { get; set; } = default!;
    public float RecaptchaThreshold { get; set; } = 1;
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
        if (string.IsNullOrEmpty(token))
            return false;

        var response = await httpClient.GetStreamAsync($"{options.VefiyAPIAddress}?secret={options.Secretkey}&response={token}&remoteip={ip}");
        var tokenResponse = await JsonSerializer.DeserializeAsync<TokenResponseModel>(response);
        if (tokenResponse is null || !tokenResponse.Success || tokenResponse.Score < options.RecaptchaThreshold)
            return false;
            
        return true;
    }
}