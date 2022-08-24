namespace CTFServer.Models.Request.Teams;

/// <summary>
/// 队伍信息
/// </summary>
public class TeamInfoModel
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
    public List<TeamUserInfoModel>? Members { get; set; } = new();

    internal static TeamInfoModel FromTeam(Team team, bool includeMembers = true)
        => new()
        {
            Id = team.Id,
            Name = team.Name,
            Bio = team.Bio,
            Avatar = team.AvatarUrl,
            Locked = team.Locked,
            Members = includeMembers ? team.Members.Select(m => new TeamUserInfoModel()
            {
                Id = m.Id,
                Bio = m.Bio,
                UserName = m.UserName,
                Avatar = m.AvatarUrl,
                Captain = m.Id == team.CaptainId,
                RealName = m.RealName,
                StudentNumber = m.StdNumber
            }).ToList() : null
        };
}