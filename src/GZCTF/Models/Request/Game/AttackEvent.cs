namespace GZCTF.Models.Request.Game;

/// <summary>
/// Public attack event broadcast to the attack animation page.
/// Represents any flag submission (accepted, rejected, first blood, etc.)
/// so the visualization can render all traffic.
/// </summary>
/// <param name="TeamName">Submitting team display name.</param>
/// <param name="TeamAvatar">Relative URL to team avatar (nullable).</param>
/// <param name="TeamScore">Team's current participation score, if available.</param>
/// <param name="ChallengeTitle">Target challenge title.</param>
/// <param name="Category">Challenge category (Web, Pwn, ...).</param>
/// <param name="Type">Submission type — drives particle color.</param>
/// <param name="Time">Submission time (UTC).</param>
public record AttackEvent(
    string TeamName,
    string? TeamAvatar,
    int? TeamScore,
    string ChallengeTitle,
    ChallengeCategory Category,
    SubmissionType Type,
    DateTimeOffset Time);
