namespace GZCTF.Models.Internal;

public class CheatCheckInfo
{
    /// <summary>
    /// Check result
    /// </summary>
    public AnswerResult AnswerResult { get; set; } = AnswerResult.WrongAnswer;

    /// <summary>
    /// Cheating team name
    /// </summary>
    public string? SubmitTeamName { get; set; }

    /// <summary>
    /// Original team owns the flag
    /// </summary>
    public string? SourceTeamName { get; set; }

    /// <summary>
    /// Cheating user
    /// </summary>
    public string? CheatUserName { get; set; }

    /// <summary>
    /// Flag text
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
