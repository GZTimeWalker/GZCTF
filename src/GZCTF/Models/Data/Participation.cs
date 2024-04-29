using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// 比赛参与信息
/// </summary>
[Index(nameof(GameId))]
[Index(nameof(TeamId))]
[Index(nameof(TeamId), nameof(GameId))]
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

    /// <summary>
    /// 队伍题解
    /// </summary>
    public LocalFile? Writeup { get; set; }

    #region Db Relationship

    /// <summary>
    /// 队伍参与的队员
    /// </summary>
    public HashSet<UserParticipation> Members { get; set; } = [];

    /// <summary>
    /// 队伍激活的题目
    /// </summary>
    public HashSet<GameChallenge> Challenges { get; set; } = [];

    /// <summary>
    /// 赛题实例
    /// </summary>
    public List<GameInstance> Instances { get; set; } = [];

    /// <summary>
    /// 提交
    /// </summary>
    public List<Submission> Submissions { get; set; } = [];

    [Required]
    public int GameId { get; set; }

    public Game Game { get; set; } = default!;

    [Required]
    public int TeamId { get; set; }

    public Team Team { get; set; } = default!;

    #endregion Db Relationship
}
