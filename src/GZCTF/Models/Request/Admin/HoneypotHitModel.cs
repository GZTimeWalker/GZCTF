using System.Net;
using System.Text.Json.Serialization;

namespace GZCTF.Models.Request.Admin;

/// <summary>
/// Honeypot hit broadcast for the admin live feed.
/// Emitted whenever a request lands on a platform-wide bait route or service.
/// </summary>
public class HoneypotHitModel
{
    [JsonPropertyName("time")]
    public DateTimeOffset Time { get; set; }

    /// <summary>
    /// Bait identifier (e.g. "/.git/config", "/wp-admin", "ssh:22").
    /// </summary>
    [JsonPropertyName("bait")]
    public string Bait { get; set; } = string.Empty;

    /// <summary>
    /// Bait category (e.g. "http", "protocol").
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("method")]
    public string? Method { get; set; }

    [JsonPropertyName("ip")]
    public IPAddress? IP { get; set; }

    [JsonPropertyName("ua")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Resolved username if the request was authenticated or recently matched by IP.
    /// </summary>
    [JsonPropertyName("user")]
    public string? UserName { get; set; }

    /// <summary>
    /// Resolved team name if attribution succeeded.
    /// </summary>
    [JsonPropertyName("team")]
    public string? TeamName { get; set; }

    /// <summary>
    /// Whether a SuspicionEvent was created for this hit.
    /// False when no participation could be attributed.
    /// </summary>
    [JsonPropertyName("attributed")]
    public bool Attributed { get; set; }
}
