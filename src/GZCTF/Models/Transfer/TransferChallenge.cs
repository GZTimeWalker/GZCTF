using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Transfer;

/// <summary>
/// Challenge configuration for transfer format
/// </summary>
public class TransferChallenge : IValidatableObject
{
    /// <summary>
    /// Original challenge ID (for mapping, not imported)
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Challenge title
    /// </summary>
    [Required(ErrorMessage = "Challenge title is required")]
    [MinLength(1, ErrorMessage = "Challenge title cannot be empty")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Challenge content (supports Markdown)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Challenge category
    /// </summary>
    [Required(ErrorMessage = "Challenge category is required")]
    public ChallengeCategory Category { get; set; } = ChallengeCategory.Misc;

    /// <summary>
    /// Challenge type
    /// </summary>
    [Required(ErrorMessage = "Challenge type is required")]
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;

    /// <summary>
    /// Whether the challenge is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Scoring configuration
    /// </summary>
    [Required(ErrorMessage = "Scoring configuration is required")]
    public ScoringSection Scoring { get; set; } = new();

    /// <summary>
    /// Limits configuration
    /// </summary>
    [Required(ErrorMessage = "Limits configuration is required")]
    public LimitsSection Limits { get; set; } = new();

    /// <summary>
    /// Flags configuration
    /// </summary>
    [Required(ErrorMessage = "Flags configuration is required")]
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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Note: Flags may be completely unset for imported challenges.
        // This is acceptable since imported challenges are disabled by default
        // and require manual enablement after proper flag configuration.

        if (Type.IsAttachment())
            yield break;

        // Validate container challenges have container config
        if (Container is null)
            yield return new ValidationResult("Container challenges must have container configuration", [nameof(Container)]);
    }
}

public class ScoringSection
{
    /// <summary>
    /// Original score
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Original score must be positive")]
    public int Original { get; set; } = 1000;

    /// <summary>
    /// Minimum score rate
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "Minimum score rate must be between 0 and 1")]
    public double MinRate { get; set; } = 0.25;

    /// <summary>
    /// Difficulty coefficient
    /// </summary>
    [Range(0.0, double.MaxValue, ErrorMessage = "Difficulty coefficient must be non-negative")]
    public double Difficulty { get; set; } = 5.0;
}

public class LimitsSection
{
    /// <summary>
    /// Submission limit (0 = unlimited)
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Submission limit must be non-negative")]
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
    [MaxLength(Limits.MaxFlagTemplateLength, ErrorMessage = "Flag template is too long")]
    public string? Template { get; set; }

    /// <summary>
    /// Static flags list
    /// </summary>
    public List<StaticFlagSection>? Static { get; set; }

    // Note: Individual StaticFlagSection items are validated by their own
    // DataAnnotations attributes ([Required], [MaxLength]). TransferValidator
    // will recursively validate all items in the Static list automatically.
}

public class StaticFlagSection
{
    /// <summary>
    /// Flag value
    /// </summary>
    [Required(ErrorMessage = "Flag value is required")]
    [MaxLength(Limits.MaxFlagLength, ErrorMessage = "Flag value is too long")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Attachment for this flag (null = no attachment)
    /// </summary>
    public AttachmentSection? Attachment { get; set; }
}


public class AttachmentSection : IValidatableObject
{
    /// <summary>
    /// Attachment type (Local or Remote)
    /// </summary>
    [Required(ErrorMessage = "Attachment type is required")]
    public FileType Type { get; set; } = FileType.Local;

    /// <summary>
    /// File hash (for Local type)
    /// </summary>
    [MaxLength(Limits.FileHashLength, ErrorMessage = "File hash is too long")]
    [RegularExpression(@"^[a-fA-F0-9]{64}$", ErrorMessage = "File hash must be 64 hex characters")]
    public string? Hash { get; set; }

    /// <summary>
    /// File name
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "File size must be non-negative")]
    public long? FileSize { get; set; }

    /// <summary>
    /// Remote URL (for Remote type)
    /// </summary>
    [Url(ErrorMessage = "Remote URL is invalid")]
    public string? RemoteUrl { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Type == FileType.Local)
        {
            if (string.IsNullOrWhiteSpace(Hash))
                yield return new ValidationResult("Local attachment must have a file hash", [nameof(Hash)]);

            if (string.IsNullOrWhiteSpace(FileName))
                yield return new ValidationResult("Local attachment must have a file name", [nameof(FileName)]);

            if (FileSize is null or <= 0)
                yield return new ValidationResult("Local attachment must have a valid file size", [nameof(FileSize)]);
        }
        else if (Type == FileType.Remote)
        {
            if (string.IsNullOrWhiteSpace(RemoteUrl))
                yield return new ValidationResult("Remote attachment must have a URL", [nameof(RemoteUrl)]);

            if (string.IsNullOrWhiteSpace(FileName))
                yield return new ValidationResult("Remote attachment must have a file name", [nameof(FileName)]);
        }
        // Note: Type == FileType.None is valid (represents no attachment)
        // FileType is an enum, so no other values are possible
    }
}

public class ContainerSection
{
    /// <summary>
    /// Container image
    /// </summary>
    [Required(ErrorMessage = "Container image is required")]
    [MinLength(1, ErrorMessage = "Container image cannot be empty")]
    public string Image { get; set; } = string.Empty;

    /// <summary>
    /// Memory limit in MB
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Memory limit must be positive")]
    public int MemoryLimit { get; set; } = 64;

    /// <summary>
    /// CPU count (in 0.1 CPU units)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "CPU count must be positive")]
    public int CpuCount { get; set; } = 1;

    /// <summary>
    /// Storage limit in MB
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Storage limit must be positive")]
    public int StorageLimit { get; set; } = 256;

    /// <summary>
    /// Exposed port
    /// </summary>
    [Range(1, 65535, ErrorMessage = "Exposed port must be 1-65535")]
    public int ExposePort { get; set; } = 80;

    /// <summary>
    /// Download file name (for dynamic attachments)
    /// </summary>
    public string? FileName { get; set; }
}
