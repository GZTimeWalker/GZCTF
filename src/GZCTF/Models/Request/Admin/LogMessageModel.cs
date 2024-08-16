using System.Text.Json.Serialization;

namespace GZCTF.Models.Request.Admin;

/// <summary>
/// 日志信息（Admin）
/// </summary>
public class LogMessageModel
{
    /// <summary>
    /// 日志时间
    /// </summary>
    [JsonPropertyName("time")]
    public DateTimeOffset Time { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [JsonPropertyName("name")]
    public string? UserName { get; set; }

    [JsonPropertyName("level")]
    public string? Level { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    [JsonPropertyName("ip")]
    public string? IP { get; set; }

    /// <summary>
    /// 日志信息
    /// </summary>
    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    /// <summary>
    /// 任务状态
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
