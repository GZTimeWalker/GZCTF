using GZCTF.Models.Request.Account;

namespace GZCTF.Models.Request.Admin;

/// <summary>
/// Detailed team information for review (Admin)
/// </summary>
public class TeamWithDetailedUserInfo
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
    /// Captain Id
    /// </summary>
    public Guid CaptainId { get; set; }

    /// <summary>
    /// Team members
    /// </summary>
    public ProfileUserInfoModel[]? Members { get; set; }

    internal static TeamWithDetailedUserInfo FromTeam(Team team) =>
        new()
        {
            Id = team.Id,
            Name = team.Name,
            Bio = team.Bio,
            Avatar = team.AvatarUrl,
            Locked = team.Locked,
            CaptainId = team.CaptainId,
            Members = team.Members.Select(ProfileUserInfoModel.FromUserInfo).ToArray()
        };
}
