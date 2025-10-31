
namespace GZCTF.Models.Transfer;

/// <summary>
/// Challenge configuration for transfer format
/// </summary>
public class TransferChallenge
{
    /// <summary>
    /// Original challenge ID (for mapping, not imported)
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Challenge title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Challenge content (supports Markdown)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Challenge category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Challenge type
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Whether the challenge is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Scoring configuration
    /// </summary>
    public ScoringSection Scoring { get; set; } = new();

    /// <summary>
    /// Limits configuration
    /// </summary>
    public LimitsSection Limits { get; set; } = new();

    /// <summary>
    /// Flags configuration
    /// </summary>
    public FlagsSection Flags { get; set; } = new();

    /// <summary>
    /// Attachment configuration (null = no attachment)
    /// </summary>
    public AttachmentSection? Attachment { get; set; }

    /// <summary>
    /// Hints list (null or empty = no hints)
    /// </summary>
    public List<string>? Hints { get; set; }

    /// <summary>
    /// Container configuration (null = not a container challenge)
    /// </summary>
    public ContainerSection? Container { get; set; }
}

public class ScoringSection
{
    /// <summary>
    /// Original score
    /// </summary>
    public int Original { get; set; } = 1000;

    /// <summary>
    /// Minimum score rate
    /// </summary>
    public double MinRate { get; set; } = 0.25;

    /// <summary>
    /// Difficulty coefficient
    /// </summary>
    public double Difficulty { get; set; } = 5.0;
}

public class LimitsSection
{
    /// <summary>
    /// Submission limit (0 = unlimited)
    /// </summary>
    public int Submission { get; set; }

    /// <summary>
    /// Challenge deadline (null = no deadline)
    /// </summary>
    public DateTimeOffset? Deadline { get; set; }
}

public class FlagsSection
{
    /// <summary>
    /// Disable blood bonus for this challenge
    /// </summary>
    public bool DisableBloodBonus { get; set; }

    /// <summary>
    /// Enable traffic capture
    /// </summary>

    public bool EnableTrafficCapture { get; set; }

    /// <summary>
    /// Dynamic flag template (null = no dynamic flag)
    /// </summary>
    public string? Template { get; set; }

    /// <summary>
    /// Static flags list
    /// </summary>
    public List<StaticFlagSection>? Static { get; set; }
}

public class StaticFlagSection
{
    /// <summary>
    /// Flag value
    /// </summary>

    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Attachment for this flag (null = no attachment)
    /// </summary>
    public AttachmentSection? Attachment { get; set; }
}


public class AttachmentSection
{
    /// <summary>
    /// Attachment type (Local or Remote)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// File hash (for Local type)
    /// </summary>
    public string? Hash { get; set; }

    /// <summary>
    /// File name
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// Remote URL (for Remote type)
    /// </summary>
    public string? RemoteUrl { get; set; }
}

public class ContainerSection
{
    /// <summary>
    /// Container image
    /// </summary>
    public string Image { get; set; } = string.Empty;

    /// <summary>
    /// Memory limit in MB
    /// </summary>
    public int MemoryLimit { get; set; } = 64;

    /// <summary>
    /// CPU count (in 0.1 CPU units)
    /// </summary>
    public int CpuCount { get; set; } = 1;

    /// <summary>
    /// Storage limit in MB
    /// </summary>
    public int StorageLimit { get; set; } = 256;

    /// <summary>
    /// Exposed port
    /// </summary>
    public int ExposePort { get; set; } = 80;

    /// <summary>
    /// Download file name (for dynamic attachments)
    /// </summary>
    public string? FileName { get; set; }
}
