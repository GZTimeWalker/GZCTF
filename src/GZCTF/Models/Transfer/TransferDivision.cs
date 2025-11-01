using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Transfer;

/// <summary>
/// Division configuration for transfer format
/// </summary>
public class TransferDivision
{
    /// <summary>
    /// Division name
    /// </summary>
    [Required(ErrorMessage = "Division name is required")]
    [MinLength(1, ErrorMessage = "Division name cannot be empty")]
    [MaxLength(Limits.MaxDivisionNameLength, ErrorMessage = "Division name is too long")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Invitation code (null = no code required)
    /// </summary>
    [MaxLength(Limits.InviteTokenLength, ErrorMessage = "Invite code is too long")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Invite code contains invalid characters")]
    public string? InviteCode { get; set; }

    /// <summary>
    /// Default permissions as flag strings
    /// </summary>
    public List<string> DefaultPermissions { get; set; } = ["None"];

    /// <summary>
    /// Challenge-specific configurations
    /// </summary>
    public List<ChallengeConfigSection> ChallengeConfig { get; set; } = [];
}

public class ChallengeConfigSection
{
    /// <summary>
    /// Original challenge ID (for mapping)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Challenge ID must be positive")]
    public int ChallengeId { get; set; }

    /// <summary>
    /// Challenge-specific permissions as flag strings
    /// </summary>
    public List<string> Permissions { get; set; } = [];
}
