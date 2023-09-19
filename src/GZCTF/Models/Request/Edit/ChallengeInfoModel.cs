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
    [Required(ErrorMessage = "标题是必需的")]
    [MinLength(1, ErrorMessage = "标题过短")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目标签
    /// </summary>
    public ChallengeTag Tag { get; set; } = ChallengeTag.Misc;

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

    internal static ChallengeInfoModel FromChallenge(GameChallenge gameChallenge) =>
        new()
        {
            Id = gameChallenge.Id,
            Title = gameChallenge.Title,
            Tag = gameChallenge.Tag,
            Type = gameChallenge.Type,
            Score = gameChallenge.CurrentScore,
            MinScore = (int)Math.Floor(gameChallenge.MinScoreRate * gameChallenge.OriginalScore),
            OriginalScore = gameChallenge.OriginalScore,
            IsEnabled = gameChallenge.IsEnabled
        };
}