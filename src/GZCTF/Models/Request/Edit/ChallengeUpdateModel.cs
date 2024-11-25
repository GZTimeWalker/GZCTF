using System.ComponentModel.DataAnnotations;
using GZCTF.Extensions;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// Challenge update information (Edit)
/// </summary>
public class ChallengeUpdateModel
{
    /// <summary>
    /// Challenge title
    /// </summary>
    [MinLength(1, ErrorMessageResourceName = nameof(Resources.Program.Model_TitleTooShort),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? Title { get; set; }

    /// <summary>
    /// Challenge content
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Flag template, used to generate Flag based on Token and challenge/game information
    /// </summary>
    [MaxLength(Limits.MaxFlagTemplateLength, ErrorMessageResourceName = nameof(Resources.Program.Model_FlagTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? FlagTemplate { get; set; }

    /// <summary>
    /// Challenge category
    /// </summary>
    public ChallengeCategory? Category { get; set; }

    /// <summary>
    /// Challenge hints
    /// </summary>
    public List<string>? Hints { get; set; }

    /// <summary>
    /// Is the challenge enabled
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Unified file name
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Container image name and tag
    /// </summary>
    public string? ContainerImage { get; set; }

    /// <summary>
    /// Memory limit (MB)
    /// </summary>
    [Range(32, 1048576, ErrorMessageResourceName = nameof(Resources.Program.Model_OutOfRange),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public int? MemoryLimit { get; set; }

    /// <summary>
    /// CPU limit (0.1 CPUs)
    /// </summary>
    [Range(1, 1024, ErrorMessageResourceName = nameof(Resources.Program.Model_OutOfRange),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public int? CPUCount { get; set; }

    /// <summary>
    /// Storage limit (MB)
    /// </summary>
    [Range(128, 1048576, ErrorMessageResourceName = nameof(Resources.Program.Model_OutOfRange),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public int? StorageLimit { get; set; }

    /// <summary>
    /// Container exposed port
    /// </summary>
    public int? ContainerExposePort { get; set; }

    /// <summary>
    /// Is traffic capture enabled
    /// </summary>
    public bool? EnableTrafficCapture { get; set; }

    /// <summary>
    /// Is blood bonus disabled
    /// </summary>
    public bool? DisableBloodBonus { get; set; }

    /// <summary>
    /// Initial score
    /// </summary>
    public int? OriginalScore { get; set; }

    /// <summary>
    /// Minimum score rate
    /// </summary>
    [Range(0, 1)]
    public double? MinScoreRate { get; set; }

    /// <summary>
    /// Difficulty coefficient
    /// </summary>
    public double? Difficulty { get; set; }

    /// <summary>
    /// Check if hints are updated
    /// </summary>
    /// <param name="originalHash">Original hash</param>
    /// <returns></returns>
    internal bool IsHintUpdated(int? originalHash) =>
        Hints is not null && Hints.GetSetHashCode() != originalHash;

    /// <summary>
    /// Check if the Flag template is valid
    /// </summary>
    /// <returns></returns>
    internal bool IsValidFlagTemplate()
    {
        if (string.IsNullOrWhiteSpace(FlagTemplate))
            return false;

        return FlagTemplate.Contains("[GUID]") ||
               FlagTemplate.Contains("[TEAM_HASH]") ||
               Codec.Leet.LeetEntropy(FlagTemplate) >= 32.0;
    }
}
