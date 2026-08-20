using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data.Cyctf;

/// <summary>
/// CYCTF 赞助商
/// </summary>
[Index(nameof(GameId))]
public class Sponsor
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
    /// 赞助商简称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ShortName { get; set; } = string.Empty;

    /// <summary>
    /// 赞助商全称
    /// </summary>
    [MaxLength(200)]
    public string? FullName { get; set; }

    /// <summary>
    /// 赞助商网站
    /// </summary>
    [MaxLength(500)]
    public string? Website { get; set; }

    /// <summary>
    /// Logo URL 或文件路径
    /// </summary>
    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    /// <summary>
    /// 赞助商类型（CO_ORGANIZER, SPONSOR, PARTNER）
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = "SPONSOR";

    /// <summary>
    /// 类型标签（用于前端显示）
    /// </summary>
    [MaxLength(50)]
    public string? TypeLabel { get; set; }

    /// <summary>
    /// 排序顺序
    /// </summary>
    [Required]
    public int SortOrder { get; set; } = 0;

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
    /// 关联的 GameExtension
    /// </summary>
    [JsonIgnore]
    public GameExtension GameExtension { get; set; } = null!;

    #endregion
}
