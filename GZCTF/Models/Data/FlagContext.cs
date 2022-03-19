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
    public int FileId { get; set; }

    /// <summary>
    /// 文件哈希
    /// </summary>
    public string FileHash { get; set; } = string.Empty;


    /// <summary>
    /// 附件类型
    /// </summary>
    [Required]
    public FileType AttachmentType { get; set; } = FileType.Local;

    /// <summary>
    /// Flag 对应附件 (远程文件）
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Flag 对应附件 (本地文件）
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
