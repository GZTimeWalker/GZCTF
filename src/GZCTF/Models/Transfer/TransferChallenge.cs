using CsToml;

namespace GZCTF.Models.Transfer;

/// <summary>
/// Challenge configuration for transfer format
/// </summary>
[TomlSerializedObject(TomlNamingConvention.SnakeCase)]
public partial class TransferChallenge
{
    /// <summary>
    /// Original challenge ID (for mapping, not imported)
    /// </summary>
    [TomlValueOnSerialized]
    public int Id { get; set; }

    /// <summary>
    /// Challenge title
    /// </summary>
    [TomlValueOnSerialized]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Challenge content (supports Markdown)
    /// </summary>
    [TomlValueOnSerialized]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Challenge category
    /// </summary>
    [TomlValueOnSerialized]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Challenge type
    /// </summary>
    [TomlValueOnSerialized]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Whether the challenge is enabled
    /// </summary>
    [TomlValueOnSerialized]
    public bool Enabled { get; set; }

    /// <summary>
    /// Scoring configuration
    /// </summary>
    [TomlValueOnSerialized]
    public ScoringSection Scoring { get; set; } = new();

    /// <summary>
    /// Limits configuration
    /// </summary>
    [TomlValueOnSerialized]
    public LimitsSection Limits { get; set; } = new();

    /// <summary>
    /// Flags configuration
    /// </summary>
    [TomlValueOnSerialized]
    public FlagsSection Flags { get; set; } = new();

    /// <summary>
    /// Attachment configuration (null = no attachment)
    /// </summary>
    [TomlValueOnSerialized(NullHandling = TomlNullHandling.Ignore)]
    public AttachmentSection? Attachment { get; set; }

    /// <summary>
    /// Hints list (null or empty = no hints)
    /// </summary>
    [TomlValueOnSerialized(NullHandling = TomlNullHandling.Ignore)]
    public List<string>? Hints { get; set; }

    /// <summary>
    /// Container configuration (null = not a container challenge)
    /// </summary>
    [TomlValueOnSerialized(NullHandling = TomlNullHandling.Ignore)]
    public ContainerSection? Container { get; set; }
}

[TomlSerializedObject(TomlNamingConvention.SnakeCase)]
public partial class ScoringSection
{
    /// <summary>
    /// Original score
    /// </summary>
    [TomlValueOnSerialized]
    public int Original { get; set; } = 1000;

    /// <summary>
    /// Minimum score rate
    /// </summary>
    [TomlValueOnSerialized]
    public double MinRate { get; set; } = 0.25;

    /// <summary>
    /// Difficulty coefficient
    /// </summary>
    [TomlValueOnSerialized]
    public double Difficulty { get; set; } = 5.0;
}

[TomlSerializedObject(TomlNamingConvention.SnakeCase)]
public partial class LimitsSection
{
    /// <summary>
    /// Submission limit (0 = unlimited)
    /// </summary>
    [TomlValueOnSerialized]
    public int Submission { get; set; }

    /// <summary>
    /// Challenge deadline (null = no deadline)
    /// </summary>
    [TomlValueOnSerialized(NullHandling = TomlNullHandling.Ignore)]
    public DateTimeOffset? Deadline { get; set; }
}

[TomlSerializedObject(TomlNamingConvention.SnakeCase)]
public partial class FlagsSection
{
    /// <summary>
    /// Disable blood bonus for this challenge
    /// </summary>
    [TomlValueOnSerialized]
    public bool DisableBloodBonus { get; set; }

    /// <summary>
    /// Enable traffic capture
    /// </summary>
    [TomlValueOnSerialized]
    public bool EnableTrafficCapture { get; set; }

    /// <summary>
    /// Dynamic flag template (null = no dynamic flag)
    /// </summary>
    [TomlValueOnSerialized(NullHandling = TomlNullHandling.Ignore)]
    public string? Template { get; set; }

    /// <summary>
    /// Static flags list
    /// </summary>
    [TomlValueOnSerialized(NullHandling = TomlNullHandling.Ignore)]
    public List<StaticFlagSection>? Static { get; set; }
}

[TomlSerializedObject(TomlNamingConvention.SnakeCase)]
public partial class StaticFlagSection
{
    /// <summary>
    /// Flag value
    /// </summary>
    [TomlValueOnSerialized]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Attachment for this flag (null = no attachment)
    /// </summary>
    [TomlValueOnSerialized(NullHandling = TomlNullHandling.Ignore)]
    public AttachmentSection? Attachment { get; set; }
}

[TomlSerializedObject(TomlNamingConvention.SnakeCase)]
public partial class AttachmentSection
{
    /// <summary>
    /// Attachment type (Local or Remote)
    /// </summary>
    [TomlValueOnSerialized]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// File hash (for Local type)
    /// </summary>
    [TomlValueOnSerialized(NullHandling = TomlNullHandling.Ignore)]
    public string? Hash { get; set; }

    /// <summary>
    /// File name
    /// </summary>
    [TomlValueOnSerialized(NullHandling = TomlNullHandling.Ignore)]
    public string? FileName { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    [TomlValueOnSerialized(NullHandling = TomlNullHandling.Ignore)]
    public long? FileSize { get; set; }

    /// <summary>
    /// Remote URL (for Remote type)
    /// </summary>
    [TomlValueOnSerialized(NullHandling = TomlNullHandling.Ignore)]
    public string? RemoteUrl { get; set; }
}

[TomlSerializedObject(TomlNamingConvention.SnakeCase)]
public partial class ContainerSection
{
    /// <summary>
    /// Container image
    /// </summary>
    [TomlValueOnSerialized]
    public string Image { get; set; } = string.Empty;

    /// <summary>
    /// Memory limit in MB
    /// </summary>
    [TomlValueOnSerialized]
    public int MemoryLimit { get; set; } = 64;

    /// <summary>
    /// CPU count (in 0.1 CPU units)
    /// </summary>
    [TomlValueOnSerialized]
    public int CpuCount { get; set; } = 1;

    /// <summary>
    /// Storage limit in MB
    /// </summary>
    [TomlValueOnSerialized]
    public int StorageLimit { get; set; } = 256;

    /// <summary>
    /// Exposed port
    /// </summary>
    [TomlValueOnSerialized]
    public int ExposePort { get; set; } = 80;

    /// <summary>
    /// Download file name (for dynamic attachments)
    /// </summary>
    [TomlValueOnSerialized(NullHandling = TomlNullHandling.Ignore)]
    public string? FileName { get; set; }
}
