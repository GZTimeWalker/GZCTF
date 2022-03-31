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
    /// 附件类型
    /// </summary>
    [Required]
    public FileType AttachmentType { get; set; } = FileType.Local;

    /// <summary>
    /// Flag 对应附件 (远程文件）
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Flag 对应文件（本地文件）
    /// </summary>
    public LocalFile? LocalFile { get; set; } = default;

    /// <summary>
    /// 赛题
    /// </summary>
    public Challenge? Challenge { get; set; }

    /// <summary>
    /// 赛题Id
    /// </summary>
    public int ChallengeId { get; set; }
}
