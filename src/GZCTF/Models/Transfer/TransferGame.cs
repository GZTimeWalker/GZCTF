using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Transfer;

/// <summary>
/// Game configuration for transfer format
/// </summary>
public class TransferGame : IValidatableObject
{
    /// <summary>
    /// Game title
    /// </summary>
    [Required(ErrorMessage = "Game title is required")]
    [MinLength(1, ErrorMessage = "Game title cannot be empty")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Game summary
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Game detailed content (supports Markdown)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Whether the game is hidden
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    /// Practice mode enabled
    /// </summary>
    public bool PracticeMode { get; set; } = true;

    /// <summary>
    /// Accept teams without review
    /// </summary>
    public bool AcceptWithoutReview { get; set; }

    /// <summary>
    /// Invitation code (null = public game)
    /// </summary>
    [MaxLength(Limits.InviteTokenLength, ErrorMessage = "Invite code is too long")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Invite code contains invalid characters")]
    public string? InviteCode { get; set; }

    /// <summary>
    /// Team member count limit (0 = no limit)
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Team member count limit must be non-negative")]
    public int TeamMemberCountLimit { get; set; }

    /// <summary>
    /// Container count limit per team
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Container count limit must be positive")]
    public int ContainerCountLimit { get; set; } = 3;

    /// <summary>
    /// Game start time
    /// </summary>
    [Required(ErrorMessage = "Start time is required")]
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Game end time
    /// </summary>
    [Required(ErrorMessage = "End time is required")]
    public DateTimeOffset EndTime { get; set; }

    /// <summary>
    /// Writeup configuration
    /// </summary>
    public WriteupSection? Writeup { get; set; }

    /// <summary>
    /// Blood bonus configuration
    /// </summary>
    public BloodBonusSection? BloodBonus { get; set; }

    /// <summary>
    /// Poster hash
    /// </summary>
    [MaxLength(Limits.FileHashLength, ErrorMessage = "Poster hash is too long")]
    [RegularExpression(@"^[a-fA-F0-9]{64}$", ErrorMessage = "Poster hash must be 64 hex characters")]
    public string? PosterHash { get; set; }

    /// <summary>
    /// Game divisions
    /// </summary>
    public List<TransferDivision> Divisions { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
            yield return new ValidationResult("End time must be after start time", [nameof(EndTime)]);

        if (Writeup is not { Required: true })
            yield break;

        if (Writeup.Deadline <= EndTime)
            yield return new ValidationResult("Writeup deadline must be after game end time", [nameof(Writeup)]);

        // Note: BloodBonus range validation (0-1023) is handled by [Range] attributes on BloodBonusSection
    }
}


public class WriteupSection
{
    /// <summary>
    /// Whether writeup is required
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Writeup submission deadline
    /// </summary>
    public DateTimeOffset Deadline { get; set; }

    /// <summary>
    /// Additional note for writeup submission (supports Markdown)
    /// </summary>
    public string? Note { get; set; }

    // Note: Deadline validation is handled in TransferGame.Validate()
    // which checks if Deadline > EndTime when Required = true.
    // We allow incomplete data during import since games are created
    // in a disabled state and can be configured before enabling.
}

public class BloodBonusSection
{
    /// <summary>
    /// First blood bonus (in permille, e.g., 50 = 5%)
    /// </summary>
    [Range(0, 1023, ErrorMessage = "First blood bonus must be 0-1023 permille")]
    public int First { get; set; }

    /// <summary>
    /// Second blood bonus (in permille)
    /// </summary>
    [Range(0, 1023, ErrorMessage = "Second blood bonus must be 0-1023 permille")]
    public int Second { get; set; }

    /// <summary>
    /// Third blood bonus (in permille)
    /// </summary>
    [Range(0, 1023, ErrorMessage = "Third blood bonus must be 0-1023 permille")]
    public int Third { get; set; }
}
