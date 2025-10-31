namespace GZCTF.Models.Transfer;

/// <summary>
/// Division configuration for transfer format
/// </summary>
public class TransferDivision
{
    /// <summary>
    /// Division name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Invitation code (null = no code required)
    /// </summary>
    public string? InviteCode { get; set; }

    /// <summary>
    /// Default permissions as flag strings
    /// </summary>
    public List<string> DefaultPermissions { get; set; } = new();

    /// <summary>
    /// Challenge-specific configurations
    /// </summary>
    public List<ChallengeConfigSection> ChallengeConfig { get; set; } = new();
}

public class ChallengeConfigSection
{
    /// <summary>
    /// Original challenge ID (for mapping)
    /// </summary>
    public int ChallengeId { get; set; }

    /// <summary>
    /// Challenge-specific permissions as flag strings
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}
