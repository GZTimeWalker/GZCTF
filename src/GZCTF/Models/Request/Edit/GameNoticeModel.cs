using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// Game notice (Edit)
/// </summary>
public class GameNoticeModel
{
    /// <summary>
    /// Notice content
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_ContentRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional scheduled publish time. When provided and in the future, the notice will be
    /// held from players until this UTC time arrives. Null means publish immediately.
    /// </summary>
    public DateTimeOffset? PublishAt { get; set; }
}
