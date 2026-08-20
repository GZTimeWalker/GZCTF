using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data.Cyctf;

/// <summary>
/// CYCTF 报名记录
/// </summary>
[Index(nameof(GameId))]
[Index(nameof(TeamId))]
[Index(nameof(DivisionId))]
[Index(nameof(GameId), nameof(TeamId), nameof(DivisionId))]
public class Registration
{
    [Key]
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// 关联的比赛 ID
    /// </summary>
    [Required]
    public int GameId { get; set; }

    /// <summary>
    /// 关联的队伍 ID
    /// </summary>
    [Required]
    public int TeamId { get; set; }

    /// <summary>
    /// 关联的组别 ID
    /// </summary>
    [Required]
    public int DivisionId { get; set; }

    /// <summary>
    /// 报名状态（PENDING, APPROVED, REJECTED, CANCELLED）
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "PENDING";

    /// <summary>
    /// 报名表单数据（JSON）
    /// </summary>
    [Column(TypeName = "text")]
    public string? FormData { get; set; }

    /// <summary>
    /// 审核备注
    /// </summary>
    [Column(TypeName = "text")]
    public string? ReviewNote { get; set; }

    /// <summary>
    /// 审核人 ID
    /// </summary>
    public Guid? ReviewedBy { get; set; }

    /// <summary>
    /// 审核时间
    /// </summary>
    public DateTimeOffset? ReviewedAt { get; set; }

    /// <summary>
    /// 软删除标记
    /// </summary>
    [Required]
    public bool Deleted { get; set; } = false;

    /// <summary>
    /// 创建时间（报名时间）
    /// </summary>
    [Required]
    public DateTimeOffset CreateTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    [Required]
    public DateTimeOffset UpdateTime { get; set; } = DateTimeOffset.UtcNow;

    #region Db Relationship

    /// <summary>
    /// 关联的 Game
    /// </summary>
    [JsonIgnore]
    public Game Game { get; set; } = null!;

    /// <summary>
    /// 关联的 Team
    /// </summary>
    [JsonIgnore]
    public Team Team { get; set; } = null!;

    /// <summary>
    /// 关联的 Division
    /// </summary>
    [JsonIgnore]
    public Division Division { get; set; } = null!;

    /// <summary>
    /// 审核人
    /// </summary>
    [JsonIgnore]
    public UserInfo? Reviewer { get; set; }

    #endregion
}
