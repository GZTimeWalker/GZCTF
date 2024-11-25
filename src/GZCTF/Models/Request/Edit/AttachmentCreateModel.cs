namespace GZCTF.Models.Request.Edit;

/// <summary>
/// New attachment information (Edit)
/// </summary>
public class AttachmentCreateModel
{
    /// <summary>
    /// Attachment type
    /// </summary>
    public FileType AttachmentType { get; set; } = FileType.None;

    /// <summary>
    /// File hash (local file)
    /// </summary>
    public string? FileHash { get; set; } = string.Empty;

    /// <summary>
    /// File URL (remote file)
    /// </summary>
    public string? RemoteUrl { get; set; } = string.Empty;

    internal Attachment? ToAttachment(LocalFile? localFile) => AttachmentType switch
    {
        FileType.None => null,
        _ => new() { Type = AttachmentType, LocalFile = localFile, RemoteUrl = RemoteUrl }
    };
}
