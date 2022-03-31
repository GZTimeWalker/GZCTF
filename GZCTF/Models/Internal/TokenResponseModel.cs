using System.Text.Json.Serialization;

namespace CTFServer.Models.Internal;

/// <summary>
/// Response Model from Google Recaptcha V3 Verify API
/// </summary>
public class TokenResponseModel
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("score")]
    public decimal Score { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("error-codes")]
    public List<string> ErrorCodes { get; set; } = new();
}
