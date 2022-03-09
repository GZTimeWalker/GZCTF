using System.ComponentModel.DataAnnotations;

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
    /// Flag 对应附件 Id
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// Flag 对应附件
    /// </summary>
    public LocalFile? Attachment { get; set; }

    /// <summary>
    /// 赛题
    /// </summary>
    public Challenge? Challenge { get; set; }

    /// <summary>
    /// 赛题Id
    /// </summary>
    public int ChallengeId { get; set; }
}
