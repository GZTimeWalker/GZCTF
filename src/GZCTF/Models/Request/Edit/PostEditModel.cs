using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// Post item (Edit)
/// </summary>
public class PostEditModel
{
    /// <summary>
    /// Post title
    /// </summary>
    [MaxLength(Limits.MaxPostTitleLength, ErrorMessageResourceName = nameof(Resources.Program.Model_TitleTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? Title { get; set; }

    /// <summary>
    /// Post summary
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Post content
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Post tags
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Is pinned
    /// </summary>
    public bool? IsPinned { get; set; }
}
