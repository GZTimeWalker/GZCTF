using CsToml;

namespace GZCTF.Models.Transfer;

/// <summary>
/// Transfer package manifest
/// </summary>
[TomlSerializedObject(TomlNamingConvention.SnakeCase)]
public partial class TransferManifest
{
    /// <summary>
    /// Transfer format version
    /// </summary>
    [TomlValueOnSerialized]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Transfer format identifier
    /// </summary>
    [TomlValueOnSerialized]
    public string Format { get; set; } = "GZCTF-GAME";

    /// <summary>
    /// Export timestamp
    /// </summary>
    [TomlValueOnSerialized]
    public DateTimeOffset ExportedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Exporter version (GZCTF version)
    /// </summary>
    [TomlValueOnSerialized]
    public string ExporterVersion { get; set; } = string.Empty;

    /// <summary>
    /// Game information
    /// </summary>
    [TomlValueOnSerialized]
    public GameInfoSection Game { get; set; } = new();

    /// <summary>
    /// Export statistics
    /// </summary>
    [TomlValueOnSerialized]
    public StatisticsSection Statistics { get; set; } = new();

    /// <summary>
    /// Checksum information
    /// </summary>
    [TomlValueOnSerialized]
    public ChecksumSection Checksum { get; set; } = new();
}


[TomlSerializedObject(TomlNamingConvention.SnakeCase)]
public partial class GameInfoSection
{
    /// <summary>
    /// Original game ID (for reference only)
    /// </summary>
    [TomlValueOnSerialized]
    public int Id { get; set; }

    /// <summary>
    /// Game title
    /// </summary>
    [TomlValueOnSerialized]
    public string Title { get; set; } = string.Empty;
}

[TomlSerializedObject(TomlNamingConvention.SnakeCase)]
public partial class StatisticsSection
{
    /// <summary>
    /// Number of challenges
    /// </summary>
    [TomlValueOnSerialized]
    public int ChallengeCount { get; set; }

    /// <summary>
    /// Number of divisions
    /// </summary>
    [TomlValueOnSerialized]
    public int DivisionCount { get; set; }

    /// <summary>
    /// Total number of flags
    /// </summary>
    [TomlValueOnSerialized]
    public int TotalFlags { get; set; }

    /// <summary>
    /// Total number of files
    /// </summary>
    [TomlValueOnSerialized]
    public int TotalFiles { get; set; }

    /// <summary>
    /// Total size in bytes
    /// </summary>
    [TomlValueOnSerialized]
    public long TotalFileSize { get; set; }
}

[TomlSerializedObject(TomlNamingConvention.SnakeCase)]
public partial class ChecksumSection
{
    /// <summary>
    /// Checksum algorithm (SHA256)
    /// </summary>
    [TomlValueOnSerialized]
    public string Algorithm { get; set; } = "SHA256";
}
