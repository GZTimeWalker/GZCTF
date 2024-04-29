using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Info;

/// <summary>
/// 文章详细内容
/// </summary>
public class PostDetailModel
{
    /// <summary>
    /// 文章 Id
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 通知标题
    /// </summary>
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 文章总结
    /// </summary>
    [Required]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 文章内容
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 是否置顶
    /// </summary>
    [Required]
    public bool IsPinned { get; set; }

    /// <summary>
    /// 文章标签
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// 作者头像
    /// </summary>
    public string? AuthorAvatar { get; set; }

    /// <summary>
    /// 作者名称
    /// </summary>
    public string? AuthorName { get; set; }

    /// <summary>
    /// 发布时间
    /// </summary>
    [Required]
    public DateTimeOffset Time { get; set; } = DateTimeOffset.UtcNow;

    internal static PostDetailModel FromPost(Post post) =>
        new()
        {
            Id = post.Id,
            Title = post.Title,
            Tags = post.Tags,
            IsPinned = post.IsPinned,
            Summary = post.Summary,
            Content = post.Content,
            Time = post.UpdateTimeUtc,
            AuthorAvatar = post.Author?.AvatarUrl,
            AuthorName = post.Author?.UserName
        };
}
