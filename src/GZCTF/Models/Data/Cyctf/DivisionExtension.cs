using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data.Cyctf;

/// <summary>
/// CYCTF Division Extension - 存储组别的 CYCTF 特有字段
/// </summary>
[Index(nameof(DivisionId))]
public class DivisionExtension
{
    [Key]
    [Required]
    public int DivisionId { get; set; }

    /// <summary>
    /// 最小队伍人数
    /// </summary>
    public int? MinTeamSize { get; set; }

    /// <summary>
    /// 最大队伍人数
    /// </summary>
    public int? MaxTeamSize { get; set; }

    /// <summary>
    /// 报名自定义字段配置（JSON）
    /// </summary>
    [Column(TypeName = "text")]
    public string? RegistrationFields { get; set; }

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
    /// 关联的 Division
    /// </summary>
    [JsonIgnore]
    public Division Division { get; set; } = null!;

    #endregion
}
