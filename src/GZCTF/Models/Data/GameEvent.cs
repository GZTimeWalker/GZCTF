using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using GZCTF.Models.Internal;
using Microsoft.Extensions.Localization;

namespace GZCTF.Models.Data;

/// <summary>
/// Game event, recorded but not sent to the client.
/// Information includes flag submission, container start/stop, cheating, and score changes.
/// </summary>
public class GameEvent : FormattableData<EventType>
{
    [Key]
    [JsonIgnore]
    public int Id { get; set; }

    /// <summary>
    /// Publish time
    /// </summary>
    [Required]
    [JsonPropertyName("time")]
    public DateTimeOffset PublishTimeUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Related username
    /// </summary>
    [JsonPropertyName("user")]
    public string UserName => User?.UserName ?? string.Empty;

    /// <summary>
    /// Related team name
    /// </summary>
    [JsonPropertyName("team")]
    public string TeamName => Team?.Name ?? string.Empty;

    [JsonIgnore]
    public Guid? UserId { get; set; }

    [JsonIgnore]
    public UserInfo? User { get; set; }

    [JsonIgnore]
    public int TeamId { get; set; }

    [JsonIgnore]
    public Team? Team { get; set; }

    [JsonIgnore]
    public int GameId { get; set; }

    [JsonIgnore]
    public Game? Game { get; set; }

    internal static GameEvent FromSubmission(Submission submission, SubmissionType type, AnswerResult ans,
        IStringLocalizer<Program> localizer) =>
        new()
        {
            TeamId = submission.TeamId,
            UserId = submission.UserId,
            GameId = submission.GameId,
            Type = EventType.FlagSubmit,
            Values =
            [
                ans.ToString(),
                submission.Answer,
                submission.ChallengeName,
                submission.ChallengeId.ToString()
            ]
        };
}
