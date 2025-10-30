using CsToml;

namespace GZCTF.Models.Transfer;

/// <summary>
/// Game configuration for transfer format
/// </summary>
[TomlSerializedObject(TomlNamingConvention.SnakeCase)]
public partial class TransferGame
{
    /// <summary>
    /// Game title
    /// </summary>
    [TomlValueOnSerialized]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Game summary
    /// </summary>
    [TomlValueOnSerialized]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Game detailed content (supports Markdown)
    /// </summary>
    [TomlValueOnSerialized]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Whether the game is hidden
    /// </summary>
    [TomlValueOnSerialized]
    public bool Hidden { get; set; }

    /// <summary>
    /// Practice mode enabled
    /// </summary>
    [TomlValueOnSerialized]
    public bool PracticeMode { get; set; } = true;

    /// <summary>
    /// Accept teams without review
    /// </summary>
    [TomlValueOnSerialized]
    public bool AcceptWithoutReview { get; set; }

    /// <summary>
    /// Invitation code (null = public game)
    /// </summary>
    [TomlValueOnSerialized(NullHandling = TomlNullHandling.Ignore)]
    public string? InviteCode { get; set; }

    /// <summary>
    /// Team member count limit (0 = no limit)
    /// </summary>
    [TomlValueOnSerialized]
    public int TeamMemberCountLimit { get; set; }

    /// <summary>
    /// Container count limit per team
    /// </summary>
    [TomlValueOnSerialized]
    public int ContainerCountLimit { get; set; } = 3;

    /// <summary>
    /// Game start time
    /// </summary>
    [TomlValueOnSerialized]
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Game end time
    /// </summary>
    [TomlValueOnSerialized]
    public DateTimeOffset EndTime { get; set; }

    /// <summary>
    /// Writeup configuration
    /// </summary>
    [TomlValueOnSerialized(NullHandling = TomlNullHandling.Ignore)]
    public WriteupSection? Writeup { get; set; }

    /// <summary>
    /// Blood bonus configuration
    /// </summary>
    [TomlValueOnSerialized(NullHandling = TomlNullHandling.Ignore)]
    public BloodBonusSection? BloodBonus { get; set; }

    /// <summary>
    /// Poster hash
    /// </summary>
    [TomlValueOnSerialized(NullHandling = TomlNullHandling.Ignore)]
    public string? PosterHash { get; set; }
}


[TomlSerializedObject(TomlNamingConvention.SnakeCase)]
public partial class WriteupSection
{
    /// <summary>
    /// Whether writeup is required
    /// </summary>
    [TomlValueOnSerialized]
    public bool Required { get; set; }

    /// <summary>
    /// Writeup submission deadline
    /// </summary>
    [TomlValueOnSerialized]
    public DateTimeOffset Deadline { get; set; }

    /// <summary>
    /// Additional note for writeup submission (supports Markdown)
    /// </summary>
    [TomlValueOnSerialized(NullHandling = TomlNullHandling.Ignore)]
    public string? Note { get; set; }
}

[TomlSerializedObject(TomlNamingConvention.SnakeCase)]
public partial class BloodBonusSection
{
    /// <summary>
    /// First blood bonus (in permille, e.g., 50 = 5%)
    /// </summary>
    [TomlValueOnSerialized]
    public int First { get; set; }

    /// <summary>
    /// Second blood bonus (in permille)
    /// </summary>
    [TomlValueOnSerialized]
    public int Second { get; set; }

    /// <summary>
    /// Third blood bonus (in permille)
    /// </summary>
    [TomlValueOnSerialized]
    public int Third { get; set; }
}
