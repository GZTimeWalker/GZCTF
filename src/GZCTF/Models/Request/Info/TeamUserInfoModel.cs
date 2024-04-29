using System.Text.Json.Serialization;

namespace GZCTF.Models.Request.Info;

/// <summary>
/// 队员信息
/// </summary>
public class TeamUserInfoModel
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 签名
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// 头像链接
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 是否是队长
    /// </summary>
    public bool Captain { get; set; }

    /// <summary>
    /// 真实姓名，用于生成积分榜
    /// </summary>
    [JsonIgnore]
    public string? RealName { get; set; }

    /// <summary>
    /// 学号，用于生成积分榜
    /// </summary>
    [JsonIgnore]
    public string? StudentNumber { get; set; }
}
