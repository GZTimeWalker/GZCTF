using System.Text.Json.Serialization;

namespace GZCTF.Models.Response.Admin;

/// <summary>Per-challenge health stats for admin dashboard.</summary>
public class ChallengeHealthModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("solveCount")]
    public int SolveCount { get; set; }

    [JsonPropertyName("wrongCount")]
    public int WrongCount { get; set; }

    [JsonPropertyName("totalAttempts")]
    public int TotalAttempts => SolveCount + WrongCount;

    [JsonPropertyName("wrongRate")]
    public double WrongRate => TotalAttempts == 0 ? 0.0 : Math.Round((double)WrongCount / TotalAttempts * 100, 1);

    /// <summary>Time of the first accepted submission for this challenge, null if unsolved.</summary>
    [JsonPropertyName("firstSolveTime")]
    public DateTimeOffset? FirstSolveTime { get; set; }

    /// <summary>Number of distinct teams that solved it.</summary>
    [JsonPropertyName("solveTeamCount")]
    public int SolveTeamCount { get; set; }
}
