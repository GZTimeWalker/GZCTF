using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Request.Edit;

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
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// 题目分值
    /// </summary>
    public int Score { get; set; } = 500;

    /// <summary>
    /// 最低分值
    /// </summary>
    public int MinScore { get; set; } = 0;

    /// <summary>
    /// 最初分值
    /// </summary>
    public int OriginalScore { get; set; } = 500;

    public static ChallengeInfoModel FromChallenge(Challenge challenge)
        => new()
        {
            Id = challenge.Id,
            Title = challenge.Title,
            Tag = challenge.Tag,
            Type = challenge.Type,
            Score = challenge.CurrentScore,
            MinScore = (int)Math.Floor(challenge.MinScoreRate * challenge.OriginalScore),
            OriginalScore = challenge.OriginalScore,
            IsEnabled = challenge.IsEnabled
        };
}