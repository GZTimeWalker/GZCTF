using System.Text.Json.Serialization;

namespace CTFServer.Models.Request.Admin;

/// <summary>
/// 日志信息
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
}
