using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// 基础题目信息（Edit）
/// </summary>
public class ChallengeInfoModel
{
    /// <summary>
    /// 题目Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 题目名称
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_TitleRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    [MinLength(1, ErrorMessageResourceName = nameof(Resources.Program.Model_TitleTooShort),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目类别
    /// </summary>
    public ChallengeCategory Category { get; set; } = ChallengeCategory.Misc;

    /// <summary>
    /// 题目类型
    /// </summary>
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;

    /// <summary>
    /// 是否启用题目
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 题目分值
    /// </summary>
    public int Score { get; set; } = 500;

    /// <summary>
    /// 最低分值
    /// </summary>
    public int MinScore { get; set; }

    /// <summary>
    /// 最初分值
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
