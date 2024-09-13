using GZCTF.Models.Request.Shared;

namespace GZCTF.Models.Request.Game;

/// <summary>
/// 题目详细信息
/// </summary>
public class ChallengeDetailModel
{
    /// <summary>
    /// 题目 Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 题目名称
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目类别
    /// </summary>
    public ChallengeCategory Category { get; set; } = ChallengeCategory.Misc;

    /// <summary>
    /// 题目提示
    /// </summary>
    public List<string>? Hints { get; set; }

    /// <summary>
    /// 题目当前分值
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// 题目类型
    /// </summary>
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;

    /// <summary>
    /// Flag 上下文
    /// </summary>
    public ClientFlagContext Context { get; set; } = default!;

    internal static ChallengeDetailModel FromInstance(GameInstance gameInstance) =>
        new()
        {
            Id = gameInstance.Challenge.Id,
            Content = gameInstance.Challenge.Content,
            Hints = gameInstance.Challenge.Hints,
            Score = gameInstance.Challenge.CurrentScore,
            Category = gameInstance.Challenge.Category,
            Title = gameInstance.Challenge.Title,
            Type = gameInstance.Challenge.Type,
            Context = new()
            {
                InstanceEntry = gameInstance.Container?.Entry,
                CloseTime = gameInstance.Container?.ExpectStopAt,
                Url = gameInstance.AttachmentUrl,
                FileSize = gameInstance.Attachment?.FileSize
            }
        };
}
