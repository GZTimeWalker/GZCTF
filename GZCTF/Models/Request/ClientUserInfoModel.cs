using System.Text.Json.Serialization;

namespace CTFServer.Models.Request;

public class ClientUserInfoModel
{
    /// <summary>
    /// 用户名
    /// </summary>
    [JsonPropertyName("name")]
    public string UserName {  get; set; } = string.Empty;

    /// <summary>
    /// 个性签名
    /// </summary>
    [JsonPropertyName("descr")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 学号
    /// </summary>
    [JsonPropertyName("studentId")]
    public string? StudentId { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    [JsonPropertyName("realName")]
    public string? RealName { get; set; }

    /// <summary>
    /// 是否为中山大学学生
    /// </summary>
    [JsonPropertyName("isSYSU")]
    public bool IsSYSU { get; set; } = false;

    /// <summary>
    /// 手机号
    /// </summary>
    [JsonPropertyName("phone")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}
