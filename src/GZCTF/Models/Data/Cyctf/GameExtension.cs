using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data.Cyctf;

/// <summary>
/// CYCTF Game Extension - 存储比赛的 CYCTF 特有字段
/// </summary>
[Index(nameof(GameId))]
public class GameExtension
{
    [Key]
    [Required]
    public int GameId { get; set; }

    /// <summary>
    /// 报名开始时间
    /// </summary>
    [Required]
    public DateTimeOffset RegistrationStartTime { get; set; }

    /// <summary>
    /// 报名结束时间
    /// </summary>
    [Required]
    public DateTimeOffset RegistrationEndTime { get; set; }

    /// <summary>
    /// 最大报名队伍数
    /// </summary>
    public int? MaxTeams { get; set; }

    /// <summary>
    /// 是否显示报名人数
    /// </summary>
    [Required]
    public bool ShowRegistrationCount { get; set; } = true;

    /// <summary>
    /// 是否显示比赛时间
    /// </summary>
    [Required]
    public bool ShowEventTime { get; set; } = true;

    /// <summary>
    /// 当前报名队伍数
    /// </summary>
    [Required]
    public int CurrentTeams { get; set; } = 0;

    /// <summary>
    /// 邮箱白名单（JSON 数组）
    /// </summary>
    [Column(TypeName = "text")]
    public string? EmailWhitelist { get; set; }

    /// <summary>
    /// 比赛状态（DRAFT, PUBLISHED, ONGOING, ENDED）
    /// </summary>
    [MaxLength(50)]
    public string? Status { get; set; }

    /// <summary>
    /// 软删除标记
    /// </summary>
    [Required]
    public bool Deleted { get; set; } = false;

    /// <summary>
    /// 创建时间
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
    /// 赞助商列表
    /// </summary>
    [JsonIgnore]
    public List<Sponsor> Sponsors { get; set; } = [];

    /// <summary>
    /// 奖项列表
    /// </summary>
    [JsonIgnore]
    public List<Award> Awards { get; set; } = [];

    #endregion
}
