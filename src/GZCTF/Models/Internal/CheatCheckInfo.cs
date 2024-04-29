namespace GZCTF.Models.Internal;

public class CheatCheckInfo
{
    /// <summary>
    /// 检查结果
    /// </summary>
    public AnswerResult AnswerResult { get; set; } = AnswerResult.WrongAnswer;

    /// <summary>
    /// 作弊队伍名称
    /// </summary>
    public string? SubmitTeamName { get; set; }

    /// <summary>
    /// Flag 原属队伍
    /// </summary>
    public string? SourceTeamName { get; set; }

    /// <summary>
    /// 作弊用户
    /// </summary>
    public string? CheatUserName { get; set; }

    /// <summary>
    /// Flag 文本
    /// </summary>
    public string? Flag { get; set; }

    internal static CheatCheckInfo FromCheatInfo(CheatInfo info) =>
        new()
        {
            Flag = info.Submission.Answer,
            AnswerResult = AnswerResult.CheatDetected,
            SubmitTeamName = info.SubmitTeam.Team.Name,
            SourceTeamName = info.SourceTeam.Team.Name,
            CheatUserName = info.Submission.UserName
        };
}
