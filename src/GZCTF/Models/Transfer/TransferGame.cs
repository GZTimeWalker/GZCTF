namespace GZCTF.Models.Transfer;

/// <summary>
/// Game configuration for transfer format
/// </summary>
public class TransferGame
{
    /// <summary>
    /// Game title
    /// </summary>
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
    public string? InviteCode { get; set; }

    /// <summary>
    /// Team member count limit (0 = no limit)
    /// </summary>
    public int TeamMemberCountLimit { get; set; }

    /// <summary>
    /// Container count limit per team
    /// </summary>
    public int ContainerCountLimit { get; set; } = 3;

    /// <summary>
    /// Game start time
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Game end time
    /// </summary>
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
    public string? PosterHash { get; set; }

    /// <summary>
    /// Game divisions
    /// </summary>
    public List<TransferDivision> Divisions { get; set; } = [];
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
}

public class BloodBonusSection
{
    /// <summary>
    /// First blood bonus (in permille, e.g., 50 = 5%)
    /// </summary>
    public int First { get; set; }

    /// <summary>
    /// Second blood bonus (in permille)
    /// </summary>
    public int Second { get; set; }

    /// <summary>
    /// Third blood bonus (in permille)
    /// </summary>
    public int Third { get; set; }
}
