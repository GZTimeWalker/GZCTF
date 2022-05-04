namespace CTFServer.Models.Internal;

public class CheatCheckInfo
{
    /// <summary>
    /// 检查结果
    /// </summary>
    public AnswerResult AnswerResult { get; set; } = AnswerResult.WrongAnswer;

    /// <summary>
    /// 相关题目
    /// </summary>
    public Challenge? Challenge { get; set; }

    /// <summary>
    /// 作弊队伍
    /// </summary>
    public Team? CheatTeam { get; set; }

    /// <summary>
    /// Flag 原属队伍
    /// </summary>
    public Team? SourceTeam { get; set; }

    /// <summary>
    /// 作弊用户
    /// </summary>
    public UserInfo? CheatUser { get; set; }

    /// <summary>
    /// Flag 文本
    /// </summary>
    public string? Flag { get; set; }
}