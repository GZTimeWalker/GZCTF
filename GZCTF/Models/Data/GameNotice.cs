using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CTFServer.Models;

/// <summary>
/// 比赛通知，会发往客户端。
/// 信息涵盖一二三血通知、提示发布通知、题目开启通知等
/// </summary>
public class GameNotice
{
    [Key]
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
    public int GameId { get; set; }

    [JsonIgnore]
    public Game? Game { get; set; }

    public static GameNotice FromSubmission(Submission submission, NoticeType type)
        => new()
        {
            Type = type,
            Content = $"恭喜 {submission.Participation.Team.Name} 获得 ⌈{submission.Challenge.Title}⌋ 的{type.ToBloodString()}"
        };
}