using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// Participation information
/// </summary>
[Index(nameof(GameId))]
[Index(nameof(TeamId))]
[Index(nameof(TeamId), nameof(GameId))]
public class Participation
{
    [Key]
    [JsonIgnore]
    public int Id { get; set; }

    /// <summary>
    /// Participation status
    /// </summary>
    [Required]
    public ParticipationStatus Status { get; set; } = ParticipationStatus.Pending;

    /// <summary>
    /// Team token
    /// </summary>
    [Required]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Division the team belongs to
    /// </summary>
    public string? Division { get; set; }

    /// <summary>
    /// Team writeup
    /// </summary>
    public LocalFile? Writeup { get; set; }

    #region Db Relationship

    /// <summary>
    /// Members participating in the team
    /// </summary>
    public HashSet<UserParticipation> Members { get; set; } = [];

    /// <summary>
    /// Challenges activated by the team
    /// </summary>
    public HashSet<GameChallenge> Challenges { get; set; } = [];

    /// <summary>
    /// Game instances
    /// </summary>
    public List<GameInstance> Instances { get; set; } = [];

    /// <summary>
    /// Submissions
    /// </summary>
    public List<Submission> Submissions { get; set; } = [];

    /// <summary>
    /// Game ID
    /// </summary>
    [Required]
    public int GameId { get; set; }

    /// <summary>
    /// Game
    /// </summary>
    public Game Game { get; set; } = null!;

    /// <summary>
    /// Team ID
    /// </summary>
    [Required]
    public int TeamId { get; set; }

    /// <summary>
    /// Team
    /// </summary>
    public Team Team { get; set; } = null!;

    #endregion Db Relationship
}
