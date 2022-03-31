namespace CTFServer.Models.Request.Teams;

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
    /// 队伍成员
    /// </summary>
    public List<BasicUserInfoModel> Members { get; set; } = new();

    public static TeamInfoModel FromTeam(Team team)
        => new()
        {
            Id = team.Id,
            Name = team.Name,
            Bio = team.Bio,
            Avatar = $"assets/{team.AvatarHash}/avatar",
            Members = (from m in team.Members select new BasicUserInfoModel()
                       {
                           Id = m.Id,
                           Bio = m.Bio,
                           UserName = m.UserName,
                           Avatar = $"/assets/{m.AvatarHash}/avatar",
                           Captain = m.Id == team.CaptainId
                       }).ToList()
        };
}
