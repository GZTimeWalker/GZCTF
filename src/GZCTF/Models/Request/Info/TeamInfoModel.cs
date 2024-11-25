namespace GZCTF.Models.Request.Info;

/// <summary>
/// Team information
/// </summary>
public class TeamInfoModel
{
    /// <summary>
    /// Team ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Team name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Team bio
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// Avatar URL
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// Is locked
    /// </summary>
    public bool Locked { get; set; }

    /// <summary>
    /// Team members
    /// </summary>
    public List<TeamUserInfoModel>? Members { get; set; } = new();

    internal static TeamInfoModel FromTeam(Team team, bool includeMembers = true) =>
        new()
        {
            Id = team.Id,
            Name = team.Name,
            Bio = team.Bio,
            Avatar = team.AvatarUrl,
            Locked = team.Locked,
            Members = includeMembers
                ? team.Members.Select(m => new TeamUserInfoModel
                {
                    Id = m.Id,
                    Bio = m.Bio,
                    UserName = m.UserName,
                    Avatar = m.AvatarUrl,
                    Captain = m.Id == team.CaptainId,
                    RealName = m.RealName,
                    StudentNumber = m.StdNumber
                }).ToList()
                : null
        };

    internal static TeamInfoModel FromParticipation(Participation part) =>
        new()
        {
            Id = part.Team.Id,
            Name = part.Team.Name,
            Bio = part.Team.Bio,
            Avatar = part.Team.AvatarUrl,
            Locked = part.Team.Locked,
            Members = part.Members
                .Select(m => part.Team.Members.Single(u => u.Id == m.UserId))
                .Select(m => new TeamUserInfoModel
                {
                    Id = m.Id,
                    Bio = m.Bio,
                    UserName = m.UserName,
                    Avatar = m.AvatarUrl,
                    Captain = m.Id == part.Team.CaptainId,
                    RealName = m.RealName,
                    StudentNumber = m.StdNumber
                }).ToList()
        };
}
