using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models;

[Index(nameof(UserId))]
public class Submission
{
    [Key]
    [JsonIgnore]
    public int Id { get; set; }

    /// <summary>
    /// 提交的答案字符串
    /// </summary>
    [MaxLength(50)]
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// 提交的答案状态
    /// </summary>
    public AnswerResult Status { get; set; } = AnswerResult.Accepted;

    /// <summary>
    /// 答案提交的时间
    /// </summary>
    [JsonPropertyName("time")]
    public DateTimeOffset SubmitTimeUTC { get; set; } = DateTimeOffset.Parse("1970-01-01T00:00:00Z");


    #region Db Relationship

    /// <summary>
    /// 用户数据库Id
    /// </summary>
    [JsonIgnore]
    public string? UserId { get; set; }

    /// <summary>
    /// 用户
    /// </summary>
    [JsonIgnore]
    public UserInfo? User { get; set; }

    /// <summary>
    /// 参与队伍
    /// </summary>
    [JsonIgnore]
    public int ParticipationId { get; set; }

    /// <summary>
    /// 队伍
    /// </summary>
    [JsonIgnore]
    public Participation? Team { get; set; }

    /// <summary>
    /// 比赛Id
    /// </summary>
    [JsonIgnore]
    public int GameId { get; set; }

    /// <summary>
    /// 比赛
    /// </summary>
    [JsonIgnore]
    public Game? Game { get; set; }

    #endregion Db Relationship
}
