using CsToml;

namespace GZCTF.Models.Transfer;

/// <summary>
/// Division configuration for transfer format
/// </summary>
[TomlSerializedObject(TomlNamingConvention.SnakeCase)]
public partial class TransferDivision
{
    /// <summary>
    /// Division name
    /// </summary>
    [TomlValueOnSerialized]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Invitation code (null = no code required)
    /// </summary>
    [TomlValueOnSerialized]
    public string? InviteCode { get; set; }

    /// <summary>
    /// Default permissions as flag strings
    /// </summary>
    [TomlValueOnSerialized]
    public List<string> DefaultPermissions { get; set; } = new();

    /// <summary>
    /// Challenge-specific configurations
    /// </summary>
    [TomlValueOnSerialized]
    public List<ChallengeConfigSection> ChallengeConfig { get; set; } = new();
}

[TomlSerializedObject(TomlNamingConvention.SnakeCase)]
public partial class ChallengeConfigSection
{
    /// <summary>
    /// Original challenge ID (for mapping)
    /// </summary>
    [TomlValueOnSerialized]
    public int ChallengeId { get; set; }

    /// <summary>
    /// Challenge-specific permissions as flag strings
    /// </summary>
    [TomlValueOnSerialized]
    public List<string> Permissions { get; set; } = new();
}
