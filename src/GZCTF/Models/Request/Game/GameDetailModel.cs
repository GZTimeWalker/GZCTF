using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GZCTF.Models.Request.Game;

public class GameDetailModel
{
    /// <summary>
    /// Challenge information
    /// </summary>
    public Dictionary<ChallengeCategory, IEnumerable<ChallengeInfo>> Challenges { get; set; } = new();

    /// <summary>
    /// Number of challenges
    /// </summary>
    public int ChallengeCount { get; set; }

    /// <summary>
    /// Scoreboard information
    /// </summary>
    [JsonPropertyName("rank")]
    public ScoreboardItem? ScoreboardItem { get; set; }

    /// <summary>
    /// Team token
    /// </summary>
    [Required]
    public string TeamToken { get; set; } = string.Empty;

    /// <summary>
    /// Whether writeup submission is required
    /// </summary>
    [Required]
    public bool WriteupRequired { get; set; } = false;

    /// <summary>
    /// Writeup submission deadline
    /// </summary>
    [Required]
    public DateTimeOffset WriteupDeadline { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);
}
