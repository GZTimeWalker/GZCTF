using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CTFServer.Models;

public class Game
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 比赛标题
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "题目过短")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 比赛描述
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 比赛详细介绍
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 队员数量限制
    /// </summary>
    public int TeamMemberLimitCount { get; set; } = 0;

    /// <summary>
    /// 开始时间
    /// </summary>
    [Required]
    [JsonPropertyName("start")]
    public DateTimeOffset StartTimeUTC { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    /// <summary>
    /// 结束时间
    /// </summary>
    [Required]
    [JsonPropertyName("end")]
    public DateTimeOffset EndTimeUTC { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    #region Db Relationship
    /// <summary>
    /// 比赛事件
    /// </summary>
    [JsonIgnore]
    public List<GameEvent> GameEvents { get; set; } = new();

    /// <summary>
    /// 比赛通知
    /// </summary>
    [JsonIgnore]
    public List<GameNotice> GameNotices { get; set; } = new();

    /// <summary>
    /// 比赛题目
    /// </summary>
    [JsonIgnore]
    public List<Challenge> Challenges { get; set; } = new();

    /// <summary>
    /// 比赛提交
    /// </summary>
    [JsonIgnore]
    public List<Submission> Submissions { get; set; } = new();

    /// <summary>
    /// 比赛题目实例
    /// </summary>
    [JsonIgnore]
    public List<Instance> Instances { get; set; } = new();

    /// <summary>
    /// 比赛队伍
    /// </summary>
    [JsonIgnore]
    public List<Participation> Teams { get; set; } = new();
    #endregion Db Relationship
}
