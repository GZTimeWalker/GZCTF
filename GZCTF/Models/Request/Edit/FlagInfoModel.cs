namespace CTFServer.Models.Request.Edit;

/// <summary>
/// Flag 信息（Edit）
/// </summary>
public class FlagInfoModel
{
    /// <summary>
    /// Flag文本
    /// </summary>
    public string Flag { get; set; } = string.Empty;

    /// <summary>
    /// Flag 对应附件（本地文件哈希）
    /// </summary>
    public string? FileHash { get; set; } = string.Empty;

    /// <summary>
    /// Flag 对应附件 (远程文件）
    /// </summary>
    public string? Url { get; set; } = string.Empty;
}