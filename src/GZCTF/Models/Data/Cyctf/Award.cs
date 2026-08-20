using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data.Cyctf;

/// <summary>
/// CYCTF 奖项
/// </summary>
[Index(nameof(GameId))]
public class Award
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
    /// 奖项名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 奖项描述
    /// </summary>
    [Column(TypeName = "text")]
    public string? Description { get; set; }

    /// <summary>
    /// 主色调（用于前端显示）
    /// </summary>
    [MaxLength(20)]
    public string? PrimaryColor { get; set; }

    /// <summary>
    /// 次色调（用于前端显示）
    /// </summary>
    [MaxLength(20)]
    public string? SecondaryColor { get; set; }

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
