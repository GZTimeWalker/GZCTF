namespace GZCTF.Models.Transfer;

/// <summary>
/// Transfer package manifest
/// </summary>
public class TransferManifest
{
    /// <summary>
    /// Transfer format version
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Transfer format identifier
    /// </summary>
    public string Format { get; set; } = "GZCTF-GAME";

    /// <summary>
    /// Export timestamp
    /// </summary>
    public DateTimeOffset ExportedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Exporter version (GZCTF version)
    /// </summary>
    public string ExporterVersion { get; set; } = string.Empty;

    /// <summary>
    /// Game information
    /// </summary>
    public GameInfoSection Game { get; set; } = new();

    /// <summary>
    /// Export statistics
    /// </summary>
    public StatisticsSection Statistics { get; set; } = new();
}


public class GameInfoSection
{
    /// <summary>
    /// Original game ID (for reference only)
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Game title
    /// </summary>
    public string Title { get; set; } = string.Empty;
}

public class StatisticsSection
{
    /// <summary>
    /// Number of challenges
    /// </summary>
    public int ChallengeCount { get; set; }

    /// <summary>
    /// Number of divisions
    /// </summary>
    public int DivisionCount { get; set; }

    /// <summary>
    /// Total number of flags
    /// </summary>
    public int TotalFlags { get; set; }

    /// <summary>
    /// Total number of files
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Total size in bytes
    /// </summary>
    public long TotalFileSize { get; set; }
}