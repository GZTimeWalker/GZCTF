using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using MemoryPack;

namespace GZCTF.Models.Data;

/// <summary>
/// 比赛通知，会发往客户端。
/// 信息涵盖一二三血通知、提示发布通知、题目开启通知等
/// </summary>
[MemoryPackable]
public partial class GameNotice
{
    [Key]
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// 通知类型
    /// </summary>
    [Required]
    public NoticeType Type { get; set; } = NoticeType.Normal;

    /// <summary>
    /// 通知内容
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 发布时间
    /// </summary>
    [Required]
    [JsonPropertyName("time")]
    public DateTimeOffset PublishTimeUTC { get; set; } = DateTimeOffset.UtcNow;

    [JsonIgnore]
    [MemoryPackIgnore]
    public int GameId { get; set; }

    [JsonIgnore]
    [MemoryPackIgnore]
    public Game? Game { get; set; }

    internal static GameNotice FromSubmission(Submission submission, SubmissionType type)
        => new()
        {
            Type = type switch
            {
                SubmissionType.FirstBlood => NoticeType.FirstBlood,
                SubmissionType.SecondBlood => NoticeType.SecondBlood,
                SubmissionType.ThirdBlood => NoticeType.ThirdBlood,
                _ => NoticeType.Normal
            },
            GameId = submission.GameId,
            Content = $"恭喜 {submission.Team.Name} 获得 「{submission.Challenge.Title}」 的{type.ToBloodString()}"
        };
}
