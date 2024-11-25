using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using GZCTF.Models.Internal;
using MemoryPack;
using Microsoft.Extensions.Localization;

namespace GZCTF.Models.Data;

/// <summary>
/// Game notice, which will be sent to the client.
/// Information includes first, second, and third blood notifications, hint release notifications, challenging opening notifications, etc.
/// </summary>
[MemoryPackable]
public partial class GameNotice : FormattableData<NoticeType>
{
    [Key]
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Publish time
    /// </summary>
    [Required]
    [JsonPropertyName("time")]
    public DateTimeOffset PublishTimeUtc { get; set; } = DateTimeOffset.UtcNow;

    [JsonIgnore]
    [MemoryPackIgnore]
    public int GameId { get; set; }

    [JsonIgnore]
    [MemoryPackIgnore]
    public Game? Game { get; set; }

    internal static GameNotice FromSubmission(Submission submission, SubmissionType type,
        IStringLocalizer<Program> localizer) =>
        new()
        {
            Type = type switch
            {
                SubmissionType.FirstBlood => NoticeType.FirstBlood,
                SubmissionType.SecondBlood => NoticeType.SecondBlood,
                SubmissionType.ThirdBlood => NoticeType.ThirdBlood,
                _ => NoticeType.Normal
            },
            GameId = submission.GameId,
            Values = [submission.TeamName, submission.ChallengeName]
        };
}
