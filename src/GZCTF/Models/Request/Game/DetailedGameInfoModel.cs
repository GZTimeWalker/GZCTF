using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GZCTF.Models.Request.Game;

/// <summary>
/// Detailed game information, including detailed introduction and current team registration status
/// </summary>
public class DetailedGameInfoModel
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Game title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Game description
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Detailed introduction of the game
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Whether the game is hidden
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    /// List of participation divisions
    /// </summary>
    public HashSet<string>? Divisions { get; set; }

    /// <summary>
    /// Whether an invitation code is required
    /// </summary>
    public bool InviteCodeRequired { get; set; }

    /// <summary>
    /// Whether writeup submission is required
    /// </summary>
    public bool WriteupRequired { get; set; }

    /// <summary>
    /// Game poster URL
    /// </summary>
    [JsonPropertyName("poster")]
    public string? PosterUrl { get; set; } = string.Empty;

    /// <summary>
    /// Team member count limit
    /// </summary>
    [JsonPropertyName("limit")]
    public int TeamMemberCountLimit { get; set; }

    /// <summary>
    /// Number of teams registered for participation
    /// </summary>
    public int TeamCount { get; set; }

    /// <summary>
    /// Current registered division
    /// </summary>
    public string? Division { get; set; }

    /// <summary>
    /// Team name for participation
    /// </summary>
    public string? TeamName { get; set; }

    /// <summary>
    /// Whether the game is in practice mode (can still be accessed after the game ends)
    /// </summary>
    public bool PracticeMode { get; set; } = true;

    /// <summary>
    /// Team participation status
    /// </summary>
    [JsonPropertyName("status")]
    public ParticipationStatus Status { get; set; } = ParticipationStatus.Unsubmitted;

    /// <summary>
    /// Start time
    /// </summary>
    [JsonPropertyName("start")]
    public DateTimeOffset StartTimeUtc { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    /// <summary>
    /// End time
    /// </summary>
    [JsonPropertyName("end")]
    public DateTimeOffset EndTimeUtc { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    public DetailedGameInfoModel WithParticipation(Participation? part)
    {
        Status = part?.Status ?? ParticipationStatus.Unsubmitted;
        TeamName = part?.Team.Name;
        Division = part?.Division;
        return this;
    }

    internal static DetailedGameInfoModel FromGame(Data.Game game, int count) =>
        new()
        {
            Id = game.Id,
            Title = game.Title,
            Hidden = game.Hidden,
            Summary = game.Summary,
            Content = game.Content,
            PracticeMode = game.PracticeMode,
            Divisions = game.Divisions,
            InviteCodeRequired = !string.IsNullOrWhiteSpace(game.InviteCode),
            WriteupRequired = game.WriteupRequired,
            TeamCount = count,
            PosterUrl = game.PosterUrl,
            StartTimeUtc = game.StartTimeUtc,
            EndTimeUtc = game.EndTimeUtc,
            TeamMemberCountLimit = game.TeamMemberCountLimit
        };
}
