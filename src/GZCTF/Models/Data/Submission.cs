using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(UserId))]
[Index(nameof(TeamId), nameof(ChallengeId), nameof(GameId))]
public class Submission
{
    [Key]
    [JsonIgnore]
    public int Id { get; set; }

    /// <summary>
    /// 提交的答案字符串
    /// </summary>
    [MaxLength(Limits.MaxFlagLength)]
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// 提交的答案状态
    /// </summary>
    public AnswerResult Status { get; set; } = AnswerResult.Accepted;

    /// <summary>
    /// 答案提交的时间
    /// </summary>
    [JsonPropertyName("time")]
    public DateTimeOffset SubmitTimeUtc { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    /// <summary>
    /// 提交用户
    /// </summary>
    [JsonPropertyName("user")]
    public string UserName => User?.UserName ?? string.Empty;

    /// <summary>
    /// 提交队伍
    /// </summary>
    [JsonPropertyName("team")]
    public string TeamName => Team?.Name ?? string.Empty;

    /// <summary>
    /// 提交题目
    /// </summary>
    [JsonPropertyName("challenge")]
    public string ChallengeName => GameChallenge?.Title ?? string.Empty;

    #region Db Relationship

    /// <summary>
    /// 用户数据库Id
    /// </summary>
    [JsonIgnore]
    public Guid? UserId { get; set; }

    /// <summary>
    /// 用户
    /// </summary>
    [JsonIgnore]
    public UserInfo User { get; set; } = default!;

    /// <summary>
    /// 参与队伍 Id
    /// </summary>
    [JsonIgnore]
    public int TeamId { get; set; }

    /// <summary>
    /// 队伍
    /// </summary>
    [JsonIgnore]
    public Team Team { get; set; } = default!;

    /// <summary>
    /// 参与对象 Id
    /// </summary>
    [JsonIgnore]
    public int ParticipationId { get; set; }

    /// <summary>
    /// 队伍参赛对象
    /// </summary>
    [JsonIgnore]
    public Participation Participation { get; set; } = default!;

    /// <summary>
    /// 比赛Id
    /// </summary>
    [JsonIgnore]
    public int GameId { get; set; }

    /// <summary>
    /// 比赛
    /// </summary>
    [JsonIgnore]
    public Game Game { get; set; } = default!;

    /// <summary>
    /// 题目数据库Id
    /// </summary>
    [JsonIgnore]
    public int ChallengeId { get; set; }

    /// <summary>
    /// 题目
    /// </summary>
    [JsonIgnore]
    public GameChallenge GameChallenge { get; set; } = default!;

    #endregion Db Relationship
}
