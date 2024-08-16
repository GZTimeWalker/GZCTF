using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace GZCTF.Models.Internal;

/// <summary>
/// Response Model from Google Recaptcha V3 Verify API
/// </summary>
public class RecaptchaResponseModel
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("score")]
    public float Score { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("error-codes")]
    public List<string> ErrorCodes { get; set; } = [];
}

/// <summary>
/// Request Model from Cloudflare Turnstile Verify API
/// </summary>
public class TurnstileRequestModel
{
    [JsonPropertyName("secret")]
    public string Secret { get; set; } = string.Empty;

    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    // ReSharper disable once StringLiteralTypo
    [JsonPropertyName("remoteip")]
    public string RemoteIp { get; set; } = string.Empty;

    [JsonPropertyName("idempotency_key")]
    public string IdempotencyKey { get; set; } = string.Empty;
}

/// <summary>
/// Response Model from Cloudflare Turnstile Verify API
/// </summary>
public class TurnstileResponseModel
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("challenge_ts")]
    public DateTimeOffset ChallengeTimeStamp { get; set; }

    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("error-codes")]
    public List<string> ErrorCodes { get; set; } = [];
}
