using System.Text.Json.Serialization;

namespace CTFServer.Models.Request.Admin;

public class BasicUserInfoModel
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 头像链接
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 用户角色
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Role? Role { get; set; }

    public static BasicUserInfoModel FromUserInfo(UserInfo user)
    => new()
    {
        UserId = user.Id,
        UserName = user.UserName,
        Email = user.Email,
        Avatar = user.AvatarUrl,
        Role = user.Role
    };
}
