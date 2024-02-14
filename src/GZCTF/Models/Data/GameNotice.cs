using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using GZCTF.Models.Internal;
using MemoryPack;
using Microsoft.Extensions.Localization;

namespace GZCTF.Models.Data;

/// <summary>
/// 比赛通知，会发往客户端。
/// 信息涵盖一二三血通知、提示发布通知、题目开启通知等
/// </summary>
[MemoryPackable]
public partial class GameNotice : FormattableData<NoticeType>
{
    [Key]
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// 发布时间
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
            Values = [submission.Team.Name, submission.GameChallenge.Title]
        };
}
