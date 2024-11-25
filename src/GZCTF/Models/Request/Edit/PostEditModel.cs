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
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_TitleRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    [MaxLength(Limits.MaxPostTitleLength, ErrorMessageResourceName = nameof(Resources.Program.Model_TitleTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Post summary
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Post content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Post tags
    /// </summary>
    public List<string>? Tags { get; set; } = [];

    /// <summary>
    /// Is pinned
    /// </summary>
    public bool IsPinned { get; set; } = false;
}
