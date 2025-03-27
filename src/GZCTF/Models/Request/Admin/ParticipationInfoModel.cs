namespace GZCTF.Models.Request.Admin;

/// <summary>
/// Participation for review (Admin)
/// </summary>
public class ParticipationInfoModel
{
    /// <summary>
    /// Participation ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Participating team
    /// </summary>
    public TeamWithDetailedUserInfo Team { get; set; } = null!;

    /// <summary>
    /// Registered members
    /// </summary>
    public Guid[] RegisteredMembers { get; set; } = [];

    /// <summary>
    /// Division of the game
    /// </summary>
    public string? Division { get; set; }

    /// <summary>
    /// Participation status
    /// </summary>
    public ParticipationStatus Status { get; set; } = ParticipationStatus.Pending;

    internal static ParticipationInfoModel FromParticipation(Participation part) =>
        new()
        {
            Id = part.Id,
            Status = part.Status,
            Division = part.Division,
            RegisteredMembers = part.Members.Select(m => m.UserId).ToArray(),
            Team = TeamWithDetailedUserInfo.FromTeam(part.Team)
        };
}
