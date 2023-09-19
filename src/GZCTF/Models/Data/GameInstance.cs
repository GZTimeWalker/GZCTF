using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Data;

public class GameInstance : Instance
{
    /// <summary>
    /// 自定义成绩 (unused)
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// 获取实例附件
    /// </summary>
    internal Attachment? Attachment => GameChallenge.Type == ChallengeType.DynamicAttachment
        ? FlagContext?.Attachment
        : GameChallenge.Attachment;

    /// <summary>
    /// 获取实例附件链接
    /// </summary>
    internal string? AttachmentUrl => GameChallenge.Type == ChallengeType.DynamicAttachment
        ? FlagContext?.Attachment?.UrlWithName(GameChallenge.FileName)
        : GameChallenge.Attachment?.UrlWithName();

    #region Db Relationship

    public int? FlagId { get; set; }

    /// <summary>
    /// Flag 上下文对象
    /// </summary>
    public FlagContext? FlagContext { get; set; }

    [Required]
    public int ChallengeId { get; set; }

    /// <summary>
    /// 赛题对象
    /// </summary>
    public GameChallenge GameChallenge { get; set; } = default!;

    [Required]
    public int ParticipationId { get; set; }

    /// <summary>
    /// 参与队伍对象
    /// </summary>
    public Participation Participation { get; set; } = default!;

    #endregion Db Relationship
}