using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// New Flag information (Edit)
/// </summary>
public class FlagCreateModel
{
    /// <summary>
    /// Flag text
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_FlagRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    [MaxLength(Limits.MaxFlagLength, ErrorMessageResourceName = nameof(Resources.Program.Model_FlagTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Flag { get; set; } = string.Empty;

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
