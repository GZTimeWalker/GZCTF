using System.ComponentModel.DataAnnotations;
using GZCTF.Models.Request.Game;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// Challenge detailed information (Edit)
/// </summary>
public class ChallengeEditDetailModel
{
    /// <summary>
    /// Challenge Id
    /// </summary>
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
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Challenge category
    /// </summary>
    [Required]
    public ChallengeCategory Category { get; set; } = ChallengeCategory.Misc;

    /// <summary>
    /// Challenge type
    /// </summary>
    [Required]
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;

    /// <summary>
    /// Challenge hints
    /// </summary>
    public List<string> Hints { get; set; } = [];

    /// <summary>
    /// Flag template, used to generate Flag based on Token and challenge, game information
    /// </summary>
    [MaxLength(Limits.MaxFlagTemplateLength, ErrorMessageResourceName = nameof(Resources.Program.Model_FlagTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? FlagTemplate { get; set; }

    /// <summary>
    /// Is the challenge enabled
    /// </summary>
    [Required]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Number of people who passed
    /// </summary>
    [Required]
    public int AcceptedCount { get; set; }

    /// <summary>
    /// Unified file name (only for dynamic attachments)
    /// </summary>
    public string? FileName { get; set; } = string.Empty;

    /// <summary>
    /// Challenge attachment (dynamic attachments are stored in FlagInfoModel)
    /// </summary>
    public Attachment? Attachment { get; set; }

    /// <summary>
    /// Test container
    /// </summary>
    public ContainerInfoModel? TestContainer { get; set; }

    /// <summary>
    /// Challenge Flag information
    /// </summary>
    [Required]
    public List<FlagInfoModel> Flags { get; set; } = [];

    /// <summary>
    /// Image name and tag
    /// </summary>
    [Required]
    public string? ContainerImage { get; set; } = string.Empty;

    /// <summary>
    /// Memory limit (MB)
    /// </summary>
    [Required]
    public int? MemoryLimit { get; set; } = 64;

    /// <summary>
    /// CPU limit (0.1 CPUs)
    /// </summary>
    [Required]
    public int? CPUCount { get; set; } = 1;

    /// <summary>
    /// Storage limit (MB)
    /// </summary>
    [Required]
    public int? StorageLimit { get; set; } = 256;

    /// <summary>
    /// Container exposed port
    /// </summary>
    [Required]
    public int? ContainerExposePort { get; set; } = 80;

    /// <summary>
    /// Whether to record traffic
    /// </summary>
    public bool? EnableTrafficCapture { get; set; } = false;

    /// <summary>
    /// Whether to disable blood bonus
    /// </summary>
    public bool? DisableBloodBonus { get; set; } = false;

    /// <summary>
    /// Initial score
    /// </summary>
    [Required]
    public int OriginalScore { get; set; } = 500;

    /// <summary>
    /// Minimum score rate
    /// </summary>
    [Required]
    [Range(0, 1)]
    public double MinScoreRate { get; set; } = 0.25;

    /// <summary>
    /// Difficulty coefficient
    /// </summary>
    [Required]
    public double Difficulty { get; set; } = 3;

    internal static ChallengeEditDetailModel FromChallenge(GameChallenge chal) =>
        new()
        {
            Id = chal.Id,
            Title = chal.Title,
            Content = chal.Content,
            Category = chal.Category,
            Type = chal.Type,
            FlagTemplate = chal.FlagTemplate,
            Hints = chal.Hints ?? [],
            IsEnabled = chal.IsEnabled,
            ContainerImage = chal.ContainerImage,
            MemoryLimit = chal.MemoryLimit,
            CPUCount = chal.CPUCount,
            StorageLimit = chal.StorageLimit,
            ContainerExposePort = chal.ContainerExposePort,
            EnableTrafficCapture = chal.EnableTrafficCapture,
            DisableBloodBonus = chal.DisableBloodBonus,
            OriginalScore = chal.OriginalScore,
            MinScoreRate = chal.MinScoreRate,
            Difficulty = chal.Difficulty,
            FileName = chal.FileName,
            AcceptedCount = chal.AcceptedCount,
            Attachment = chal.Attachment,
            TestContainer = chal.TestContainer is null ? null : ContainerInfoModel.FromContainer(chal.TestContainer),
            Flags = chal.Flags.Select(FlagInfoModel.FromFlagContext).ToList()
        };
}
