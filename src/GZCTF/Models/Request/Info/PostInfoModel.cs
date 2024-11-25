using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Info;

/// <summary>
/// Post information
/// </summary>
public class PostInfoModel
{
    /// <summary>
    /// Post ID
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Post title
    /// </summary>
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Post summary
    /// </summary>
    [Required]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Is pinned
    /// </summary>
    [Required]
    public bool IsPinned { get; set; }

    /// <summary>
    /// Post tags
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Author avatar
    /// </summary>
    public string? AuthorAvatar { get; set; }

    /// <summary>
    /// Author name
    /// </summary>
    public string? AuthorName { get; set; }

    /// <summary>
    /// Update time
    /// </summary>
    [Required]
    public DateTimeOffset Time { get; set; } = DateTimeOffset.UtcNow;

    internal static PostInfoModel FromPost(Post post) =>
        new()
        {
            Id = post.Id,
            Title = post.Title,
            Summary = post.Summary,
            IsPinned = post.IsPinned,
            Tags = post.Tags,
            Time = post.UpdateTimeUtc,
            AuthorAvatar = post.Author?.AvatarUrl,
            AuthorName = post.Author?.UserName
        };
}
