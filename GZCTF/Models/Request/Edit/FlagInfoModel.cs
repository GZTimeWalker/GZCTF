namespace CTFServer.Models.Request.Edit;

/// <summary>
/// Flag 信息（Edit）
/// </summary>
public class FlagInfoModel
{
    /// <summary>
    /// Flag Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Flag文本
    /// </summary>
    public string Flag { get; set; } = string.Empty;

    /// <summary>
    /// 对应附件类型
    /// </summary>
    public FileType Type { get; set; } = FileType.None;

    /// <summary>
    /// Flag 对应附件（本地文件哈希）
    /// </summary>
    public string? FileHash { get; set; } = string.Empty;

    /// <summary>
    /// Flag 对应附件 (远程文件）
    /// </summary>
    public string? RemoteUrl { get; set; } = string.Empty;

    /// <summary>
    /// Flag 对应附件链接
    /// </summary>
    public string? Url { get; set; } = string.Empty;

    public static FlagInfoModel FromFlagContext(FlagContext context)
        => new()
        {
            Id = context.Id,
            Flag = context.Flag,
            Type = context.AttachmentType,
            FileHash = context.LocalFile?.Hash,
            RemoteUrl = context.RemoteUrl,
            Url = context.Url,
        };
}