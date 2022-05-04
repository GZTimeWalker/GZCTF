namespace CTFServer.Models.Request.Admin;

public class BasicUserInfoModel
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string? Id { get; set; }

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
    public Role? Role { get; set; }

    /// <summary>
    /// 所拥有的队伍
    /// </summary>
    public string? OwnTeamName { get; set; }

    public int? OwnTeamId { get; set; }

    /// <summary>
    /// 激活的队伍
    /// </summary>
    public string? ActiveTeamName { get; set; }

    public int? ActiveTeamId { get; set; }

    public static BasicUserInfoModel FromUserInfo(UserInfo user)
    => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        Email = user.Email,
        Avatar = user.AvatarUrl,
        Role = user.Role,
        OwnTeamId = user.OwnTeamId,
        OwnTeamName = user.OwnTeam?.Name,
        ActiveTeamId = user.ActiveTeamId,
        ActiveTeamName = user.ActiveTeam?.Name
    };
}