using System.Text.Json.Serialization;

namespace GZCTF.Models.Response.Game;

/// <summary>Lightweight solver entry for a single challenge — used by the challenge modal.</summary>
public class ChallengeSolverModel
{
    [JsonPropertyName("rank")]
    public int Rank { get; set; }

    [JsonPropertyName("teamName")]
    public string TeamName { get; set; } = string.Empty;

    [JsonPropertyName("teamAvatar")]
    public string? TeamAvatar { get; set; }

    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [JsonPropertyName("type")]
    public SubmissionType Type { get; set; }

    [JsonPropertyName("time")]
    public DateTimeOffset Time { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }
}
