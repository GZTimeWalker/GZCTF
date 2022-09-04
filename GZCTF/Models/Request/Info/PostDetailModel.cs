using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Request.Info;

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
    /// 文章内容
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 作者头像
    /// </summary>
    public string? AutherAvatar { get; set; }

    /// <summary>
    /// 作者名称
    /// </summary>
    public string? AutherName { get; set; }

    /// <summary>
    /// 发布时间
    /// </summary>
    [Required]
    public DateTimeOffset Time { get; set; } = DateTimeOffset.UtcNow;

    public static PostDetailModel FromPost(Post post)
        => new()
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            Time = post.UpdateTimeUTC,
            AutherAvatar = post.Auther?.AvatarUrl,
            AutherName = post.Auther?.UserName,
        };
}