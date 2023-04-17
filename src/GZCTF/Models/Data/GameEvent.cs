﻿using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CTFServer.Models;

/// <summary>
/// 比赛事件，记录但不会发往客户端。
/// 信息涵盖Flag提交信息、容器启动关闭信息、作弊信息、题目分数变更信息
/// </summary>
public class GameEvent
{
    [Key]
    [JsonIgnore]
    public int Id { get; set; }

    /// <summary>
    /// 事件类型
    /// </summary>
    [Required]
    public EventType Type { get; set; } = EventType.Normal;

    /// <summary>
    /// 事件内容
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 发布时间
    /// </summary>
    [Required]
    [JsonPropertyName("time")]
    public DateTimeOffset PublishTimeUTC { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 相关用户名
    /// </summary>
    [JsonPropertyName("user")]
    public string UserName => User?.UserName ?? string.Empty;

    /// <summary>
    /// 相关队伍名
    /// </summary>
    [JsonPropertyName("team")]
    public string TeamName => Team?.Name ?? string.Empty;

    [JsonIgnore]
    public string? UserId { get; set; }

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

    internal static GameEvent FromSubmission(Submission submission, SubmissionType type, AnswerResult ans)
        => new()
        {
            TeamId = submission.TeamId,
            UserId = submission.UserId,
            GameId = submission.GameId,
            Type = EventType.FlagSubmit,
            Content = $"[{ans.ToShortString()}] {submission.Answer}  {submission.Challenge.Title}#{submission.ChallengeId}"
        };
}