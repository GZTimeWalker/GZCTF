using System.ComponentModel.DataAnnotations;
using CTFServer.Models.Data;

namespace CTFServer.Models;

public class FlagContext
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Flag 内容
    /// </summary>
    [Required]
    public string Flag { get; set; } = string.Empty;

    /// <summary>
    /// 是否已被占用
    /// </summary>
    public bool IsOccupied { get; set; } = false;

    #region Db Relationship

    /// <summary>
    /// 附件 Id
    /// </summary>
    public int? AttachmentId { get; set; }

    /// <summary>
    /// 附件
    /// </summary>
    public Attachment? Attachment { get; set; }

    /// <summary>
    /// 赛题Id
    /// </summary>
    public int ChallengeId { get; set; }

    /// <summary>
    /// 赛题
    /// </summary>
    public Challenge? Challenge { get; set; }

    #endregion Db Relationship
}