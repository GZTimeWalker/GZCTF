using System.Text.Json.Serialization;

namespace GZCTF.Models.Request.Admin;

/// <summary>
/// Log information (Admin)
/// </summary>
public class LogMessageModel
{
    /// <summary>
    /// Log time
    /// </summary>
    [JsonPropertyName("time")]
    public DateTimeOffset Time { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    [JsonPropertyName("name")]
    public string? UserName { get; set; }

    [JsonPropertyName("level")]
    public string? Level { get; set; }

    /// <summary>
    /// IP address
    /// </summary>
    [JsonPropertyName("ip")]
    public string? IP { get; set; }

    /// <summary>
    /// Log message
    /// </summary>
    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    /// <summary>
    /// Task status
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    public static LogMessageModel FromLogModel(LogModel logInfo) =>
        new()
        {
            Time = logInfo.TimeUtc,
            Level = logInfo.Level,
            UserName = logInfo.UserName,
            IP = logInfo.RemoteIP,
            Msg = logInfo.Message,
            Status = logInfo.Status
        };
}
