using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Admin;

/// <summary>
/// Participation for review (Admin)
/// </summary>
public class ParticipationInfoModel
{
    /// <summary>
    /// Participation ID
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Participating team
    /// </summary>
    [Required]
    public TeamWithDetailedUserInfo Team { get; set; } = null!;

    /// <summary>
    /// Registered members
    /// </summary>
    [Required]
    public Guid[] RegisteredMembers { get; set; } = [];

    /// <summary>
    /// Division of the game
    /// </summary>
    public int? DivisionId { get; set; }

    /// <summary>
    /// Participation status
    /// </summary>
    [Required]
    public ParticipationStatus Status { get; set; } = ParticipationStatus.Pending;

    internal static ParticipationInfoModel FromParticipation(Participation part) =>
        new()
        {
            Id = part.Id,
            Status = part.Status,
            DivisionId = part.DivisionId,
            RegisteredMembers = part.Members.Select(m => m.UserId).ToArray(),
            Team = TeamWithDetailedUserInfo.FromTeam(part.Team)
        };
}
