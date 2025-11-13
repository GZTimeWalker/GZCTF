using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GZCTF.Models.Data;

public class Challenge
{
    [Key]
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Challenge title
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_TitleRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    [MinLength(1, ErrorMessageResourceName = nameof(Resources.Program.Model_TitleTooShort),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Challenge content
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_ContentRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Challenge category
    /// </summary>
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter<ChallengeCategory>))]
    public ChallengeCategory Category { get; set; } = ChallengeCategory.Misc;

    /// <summary>
    /// Challenge type, cannot be changed after creation
    /// </summary>
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter<ChallengeType>))]
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;

    /// <summary>
    /// Challenge hints
    /// </summary>
    public List<string>? Hints { get; set; }

    /// <summary>
    /// Whether the challenge is enabled
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// The deadline of the challenge, null means no deadline
    /// </summary>
    public DateTimeOffset? DeadlineUtc { get; set; }

    /// <summary>
    /// Maximum number of submissions allowed per team (0 = no limit)
    /// </summary>
    [Required]
    public int SubmissionLimit { get; set; }

    /// <summary>
    /// Image name and tag
    /// </summary>
    public string? ContainerImage { get; set; } = string.Empty;

    /// <summary>
    /// Memory limit (MB)
    /// </summary>
    public int? MemoryLimit { get; set; } = 64;

    /// <summary>
    /// Storage limit (MB)
    /// </summary>
    public int? StorageLimit { get; set; } = 256;

    /// <summary>
    /// CPU limit (0.1 CPUs)
    /// </summary>
    public int? CPUCount { get; set; } = 1;

    /// <summary>
    /// Container exposed port
    /// </summary>
    public int? ContainerExposePort { get; set; } = 80;

    /// <summary>
    /// Download file name, used only for dynamic attachment unified file name
    /// </summary>
    public string? FileName { get; set; } = "attachment";

    /// <summary>
    /// Concurrency token
    /// </summary>
    [JsonIgnore]
    [Timestamp]
    public uint ConcurrencyToken { get; set; }

    /// <summary>
    /// Flag template, used to generate flags based on token and challenge, game information
    /// </summary>
    [MaxLength(Limits.MaxFlagTemplateLength)]
    public string? FlagTemplate { get; set; }

    /// <summary>
    /// Generate dynamic flag for the participant
    /// </summary>
    /// <param name="part"></param>
    /// <returns></returns>
    internal string GenerateDynamicFlag(Participation part)
    {
        if (string.IsNullOrEmpty(FlagTemplate))
            return $"flag{Guid.NewGuid():B}";

        var generator = new DynamicFlagGenerator(FlagTemplate);
        return generator.GenerateWithTeamHash(() =>
        {
            var salt = $"{part.Game.TeamHashSalt}::{Id}".ToSHA256String();
            var hash = $"{salt}::{part.Token}".ToSHA256String();
            return hash[12..24];
        });
    }

    /// <summary>
    /// Directly generate dynamic flag
    /// </summary>
    internal string GenerateDynamicFlag()
    {
        if (string.IsNullOrEmpty(FlagTemplate))
            return $"flag{Guid.NewGuid():B}";

        var generator = new DynamicFlagGenerator(FlagTemplate);

        // Always use TestTeamHash for [TEAM_HASH] in parameterless version
        return generator.GenerateWithTeamHash(() => "TestTeamHash");
    }

    /// <summary>
    /// Generate test flag for admin to check the challenge
    /// </summary>
    internal string GenerateTestFlag()
    {
        if (string.IsNullOrEmpty(FlagTemplate))
            return "flag{GZCTF_dynamic_flag_test}";

        var generator = new DynamicFlagGenerator(FlagTemplate);
        return generator.GenerateTestFlag();
    }

    #region Db Relationship

    /// <summary>
    /// Challenge attachment ID
    /// </summary>
    public int? AttachmentId { get; set; }

    /// <summary>
    /// Challenge attachment (dynamic attachments are stored in FlagContext)
    /// </summary>
    public Attachment? Attachment { get; set; }

    /// <summary>
    /// Test container ID
    /// </summary>
    public Guid? TestContainerId { get; set; }

    /// <summary>
    /// Test container
    /// </summary>
    public Container? TestContainer { get; set; }

    /// <summary>
    /// List of flags for the challenge
    /// </summary>
    public List<FlagContext> Flags { get; set; } = [];

    #endregion
}
