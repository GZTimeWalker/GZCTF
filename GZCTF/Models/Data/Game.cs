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
    /// 开始时间
    /// </summary>
    [Required]
    [JsonPropertyName("start")]
    public DateTimeOffset StartTimeUTC { get; set; } = DateTimeOffset.Parse("1970-01-01T00:00:00Z");

    /// <summary>
    /// 结束时间
    /// </summary>
    [Required]
    [JsonPropertyName("end")]
    public DateTimeOffset EndTimeUTC { get; set; } = DateTimeOffset.Parse("1970-01-01T00:00:00Z");

    #region Db Relationship
    /// <summary>
    /// 比赛通知
    /// </summary>
    public List<GameNotice> Notices { get; set; } = new();

    /// <summary>
    /// 比赛题目
    /// </summary>
    public List<Challenge> Challenges { get; set; } = new();

    /// <summary>
    /// 比赛提交
    /// </summary>
    public List<Submission> Submissions { get; set; } = new();

    /// <summary>
    /// 比赛题目实例
    /// </summary>
    public List<Instance> Instances { get; set; } = new();

    /// <summary>
    /// 比赛队伍
    /// </summary>
    public List<Participation> Teams { get; set; } = new();

    /// <summary>
    /// 题目信息
    /// </summary>
    public List<Rank> Ranks { get; set; } = new();

    #endregion Db Relationship
}
