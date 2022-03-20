using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Request.Account;

public class ClientUserInfoModel
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 签名
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// 手机号码
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 头像链接
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 当前队伍
    /// </summary>
    public int ActiveTeamId { get; set; }
}
