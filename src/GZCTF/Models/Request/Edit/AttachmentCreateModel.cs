namespace CTFServer.Models.Request.Edit;

/// <summary>
/// 新建附件信息（Edit）
/// </summary>
public class AttachmentCreateModel
{
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