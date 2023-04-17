using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Request.Edit;

/// <summary>
/// 新建 Flag 信息（Edit）
/// </summary>
public class FlagCreateModel
{
    /// <summary>
    /// Flag文本
    /// </summary>
    [Required]
    [MaxLength(125, ErrorMessage = "flag 字符串过长")]
    public string Flag { get; set; } = string.Empty;

    /// <summary>
    /// 附件类型
    /// </summary>
    public FileType AttachmentType { get; set; } = FileType.None;

    /// <summary>
    /// 文件哈希（本地文件）
    /// </summary>
    public string? FileHash { get; set; } = string.Empty;

    /// <summary>
    /// 文件 Url（远程文件）
    /// </summary>
    public string? RemoteUrl { get; set; } = string.Empty;
}