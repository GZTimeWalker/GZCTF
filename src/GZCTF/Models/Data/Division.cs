using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MemoryPack;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[MemoryPackable]
[Index(nameof(GameId))]
public partial class Division
{
    [Key]
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// The game ID this division belongs to.
    /// </summary>
    [Required]
    [JsonIgnore]
    public int GameId { get; set; }

    /// <summary>
    /// The name of the division.
    /// </summary>
    [Required]
    [MaxLength(Limits.MaxDivisionNameLength)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Invitation code for joining the division.
    /// </summary>
    [MaxLength(Limits.InviteTokenLength)]
    public string? InviteCode { get; set; }

    /// <summary>
    /// Permissions associated with the division.
    /// </summary>
    public GamePermission DefaultPermissions { get; set; } = GamePermission.All;

    #region Db Relationship

    /// <summary>
    /// Challenge configs for this division.
    /// </summary>
    [JsonIgnore]
    public List<DivisionChallengeConfig> ChallengeConfigs { get; set; } = [];

    /// <summary>
    /// The game this division belongs to.
    /// </summary>
    [JsonIgnore]
    [MemoryPackIgnore]
    public Game? Game { get; set; }

    #endregion
}

[MemoryPackable]
[PrimaryKey(nameof(ChallengeId), nameof(DivisionId))]
public partial class DivisionChallengeConfig
{
    [Required]
    public int DivisionId { get; set; }

    [Required]
    public int ChallengeId { get; set; }

    public GamePermission Permissions { get; set; } = GamePermission.All;

    #region Db Relationship

    [MemoryPackIgnore]
    public Division? Division { get; set; }

    [MemoryPackIgnore]
    public GameChallenge? Challenge { get; set; }

    #endregion
}
