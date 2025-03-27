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
    /// Number of people who solved the challenge
    /// </summary>
    [Required]
    public int AcceptedCount { get; set; }

    /// <summary>
    /// Number of submissions
    /// </summary>
    [Required]
    public int SubmissionCount { get; set; }

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
    /// Concurrency check
    /// </summary>
    [JsonIgnore]
    [ConcurrencyCheck]
    public Guid ConcurrencyStamp { get; set; }

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

        if (FlagTemplate.Contains("[GUID]"))
            return FlagTemplate.Replace("[GUID]", Guid.NewGuid().ToString("D"));

        if (!FlagTemplate.Contains("[TEAM_HASH]"))
            return Codec.Leet.LeetFlag(FlagTemplate);

        var flag = FlagTemplate;
        if (FlagTemplate.StartsWith("[LEET]"))
            flag = Codec.Leet.LeetFlag(FlagTemplate[6..]);

        if (FlagTemplate.StartsWith("[CLEET]"))
            flag = Codec.Leet.LeetFlag(FlagTemplate[7..], true);

        //   Using the signature private key of the game to generate a hash for the
        // team is not a wise and sufficiently secure choice. Moreover, this private
        // key should not exist outside any backend systems, even if it is encrypted
        // with an XOR key in a configuration file or provided to the organizers (admin)
        // for third-party flag calculation and external distribution.
        //   To address this issue, one possible solution is to use a salted hash of
        // the private key as the salt for the team's hash.
        //   To prevent specific challenges from leaking hash salts, leading to the
        // risk of being able to compute other challenges' flags, the salt is hashed
        // with the challenge ID to generate a unique salt for each challenge.
        var salt = $"{part.Game.TeamHashSalt}::{Id}".ToSHA256String();
        var hash = $"{salt}::{part.Token}".ToSHA256String();
        return flag.Replace("[TEAM_HASH]", hash[12..24]);
    }

    /// <summary>
    /// Directly generate dynamic flag
    /// </summary>
    internal string GenerateDynamicFlag()
    {
        if (string.IsNullOrEmpty(FlagTemplate))
            return $"flag{Guid.NewGuid():B}";

        var guid = Guid.NewGuid();
        if (FlagTemplate.Contains("[GUID]"))
            return FlagTemplate.Replace("[GUID]", guid.ToString("D"));

        if (!FlagTemplate.Contains("[TEAM_HASH]"))
            return Codec.Leet.LeetFlag(FlagTemplate);

        var flag = FlagTemplate;
        if (FlagTemplate.StartsWith("[LEET]"))
            flag = Codec.Leet.LeetFlag(FlagTemplate[6..]);

        if (FlagTemplate.StartsWith("[CLEET]"))
            flag = Codec.Leet.LeetFlag(FlagTemplate[7..], true);

        return flag.Replace("[TEAM_HASH]", guid.ToString("N")[..12]);
    }

    /// <summary>
    /// Generate test flag for admin to check the challenge
    /// </summary>
    internal string GenerateTestFlag()
    {
        if (string.IsNullOrEmpty(FlagTemplate))
            return "flag{GZCTF_dynamic_flag_test}";

        if (FlagTemplate.Contains("[GUID]"))
            return FlagTemplate.Replace("[GUID]", Guid.NewGuid().ToString("D"));

        if (!FlagTemplate.Contains("[TEAM_HASH]"))
            return Codec.Leet.LeetFlag(FlagTemplate);

        var flag = FlagTemplate;
        if (FlagTemplate.StartsWith("[LEET]"))
            flag = Codec.Leet.LeetFlag(FlagTemplate[6..]);

        if (FlagTemplate.StartsWith("[CLEET]"))
            flag = Codec.Leet.LeetFlag(FlagTemplate[7..], true);

        return flag.Replace("[TEAM_HASH]", "TestTeamHash");
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
