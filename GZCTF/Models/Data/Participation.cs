using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CTFServer.Models;

public class Participation
{
    [Key]
    [JsonIgnore]
    public int Id { get; set; }

    /// <summary>
    /// 参与状态
    /// </summary>
    [Required]
    public ParticipationStatus Status { get; set; } = ParticipationStatus.Pending;

    /// <summary>
    /// 队伍 Token
    /// </summary>
    [Required]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 参赛所属组织
    /// </summary>
    public string? Organization { get; set; }

    #region Db Relationship

    /// <summary>
    /// 队伍参与的队员
    /// </summary>
    public HashSet<UserParticipation> Members { get; set; } = new();

    /// <summary>
    /// 队伍激活的题目
    /// </summary>
    public HashSet<Challenge> Challenges { get; set; } = new();

    /// <summary>
    /// 赛题实例
    /// </summary>
    public List<Instance> Instances { get; set; } = new();

    /// <summary>
    /// 提交
    /// </summary>
    public List<Submission> Submissions { get; set; } = new();

    [Required]
    public int GameId { get; set; }

    public Game Game { get; set; } = default!;

    [Required]
    public int TeamId { get; set; }

    public Team Team { get; set; } = default!;

    #endregion Db Relationship
}