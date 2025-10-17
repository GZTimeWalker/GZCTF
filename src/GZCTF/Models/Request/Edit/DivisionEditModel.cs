using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Edit;

public class DivisionCreateModel
{
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
    public GamePermission? DefaultPermissions { get; set; } = GamePermission.All;

    /// <summary>
    /// Challenge configs for this division.
    /// </summary>
    public HashSet<DivisionChallengeConfigModel>? ChallengeConfigs { get; set; } = [];
}

public class DivisionEditModel
{
    /// <summary>
    /// The name of the division.
    /// </summary>
    [MaxLength(Limits.MaxDivisionNameLength)]
    public string? Name { get; set; } = string.Empty;

    /// <summary>
    /// Invitation code for joining the division.
    /// </summary>
    [MaxLength(Limits.InviteTokenLength)]
    public string? InviteCode { get; set; }

    /// <summary>
    /// Permissions associated with the division.
    /// </summary>
    public GamePermission? DefaultPermissions { get; set; }

    /// <summary>
    /// Challenge configs for this division.
    /// </summary>
    public HashSet<DivisionChallengeConfigModel>? ChallengeConfigs { get; set; }
}

public record DivisionChallengeConfigModel
{
    /// <summary>
    /// Challenge ID
    /// </summary>
    [Required]
    public int ChallengeId { get; set; }

    /// <summary>
    /// Challenge Specific Permissions
    /// </summary>
    public GamePermission Permissions { get; set; } = GamePermission.All;
}
