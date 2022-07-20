namespace CTFServer.Models.Request.Admin;

/// <summary>
/// 用户信息（Admin）
/// </summary>
public class UserInfoModel
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
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 学号
    /// </summary>
    public string? StdNumber { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 签名
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// 注册时间
    /// </summary>
    public DateTimeOffset RegisterTimeUTC { get; set; }

    /// <summary>
    /// 用户最近访问时间
    /// </summary>
    public DateTimeOffset LastVisitedUTC { get; set; }

    /// <summary>
    /// 用户最近访问IP
    /// </summary>
    public string IP { get; set; } = "0.0.0.0";

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

    public static UserInfoModel FromUserInfo(UserInfo user)
        => new()
        {
            Id = user.Id,
            IP = user.IP,
            Bio = user.Bio,
            Role = user.Role,
            Email = user.Email,
            Phone = user.PhoneNumber,
            Avatar = user.AvatarUrl,
            RealName = user.RealName,
            UserName = user.UserName,
            StdNumber = user.StdNumber,
            LastVisitedUTC = user.LastVisitedUTC,
            RegisterTimeUTC = user.RegisterTimeUTC,
            OwnTeamId = user.OwnTeamId,
            OwnTeamName = user.OwnTeam?.Name,
            ActiveTeamId = user.ActiveTeamId,
            ActiveTeamName = user.ActiveTeam?.Name
        };
}