using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// Basic challenge information (Edit)
/// </summary>
public class ChallengeInfoModel
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
    /// Challenge category
    /// </summary>
    public ChallengeCategory Category { get; set; } = ChallengeCategory.Misc;

    /// <summary>
    /// Challenge type
    /// </summary>
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;

    /// <summary>
    /// Is the challenge enabled
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Challenge score
    /// </summary>
    public int Score { get; set; } = 500;

    /// <summary>
    /// Minimum score
    /// </summary>
    public int MinScore { get; set; }

    /// <summary>
    /// Original score
    /// </summary>
    public int OriginalScore { get; set; } = 500;

    internal static ChallengeInfoModel FromChallenge(GameChallenge challenge) =>
        new()
        {
            Id = challenge.Id,
            Title = challenge.Title,
            Category = challenge.Category,
            Type = challenge.Type,
            Score = challenge.CurrentScore,
            MinScore = (int)Math.Floor(challenge.MinScoreRate * challenge.OriginalScore),
            OriginalScore = challenge.OriginalScore,
            IsEnabled = challenge.IsEnabled
        };
}
