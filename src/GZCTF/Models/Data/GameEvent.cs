using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using GZCTF.Models.Internal;
using Microsoft.Extensions.Localization;

namespace GZCTF.Models.Data;

/// <summary>
/// 比赛事件，记录但不会发往客户端。
/// 信息涵盖Flag提交信息、容器启动关闭信息、作弊信息、题目分数变更信息
/// </summary>
public class GameEvent : FormattableData<EventType>
{
    [Key]
    [JsonIgnore]
    public int Id { get; set; }

    /// <summary>
    /// 发布时间
    /// </summary>
    [Required]
    [JsonPropertyName("time")]
    public DateTimeOffset PublishTimeUtc { get; set; } = DateTimeOffset.UtcNow;

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
                submission.GameChallenge.Title,
                submission.ChallengeId.ToString()
            ]
        };
}
