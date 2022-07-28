using CTFServer.Models.Request.Account;

namespace CTFServer.Models.Request.Admin;

/// <summary>
/// 比赛队伍详细信息，用于审核查看（Admin）
/// </summary>
public class TeamWithDetailedUserInfo
{
    /// <summary>
    /// 队伍 Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 队伍名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 队伍签名
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// 头像链接
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 是否锁定
    /// </summary>
    public bool Locked { get; set; }

    /// <summary>
    /// 队伍成员
    /// </summary>
    public ProfileUserInfoModel[]? Members { get; set; }

    public static TeamWithDetailedUserInfo FromTeam(Team team)
        => new()
        {
            Id = team.Id,
            Name = team.Name,
            Bio = team.Bio,
            Avatar = team.AvatarUrl,
            Locked = team.Locked,
            Members = team.Members.Select(u => ProfileUserInfoModel.FromUserInfo(u)).ToArray()
        };
}