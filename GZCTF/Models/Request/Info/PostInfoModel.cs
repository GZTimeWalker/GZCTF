using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Request.Info;

/// <summary>
/// 文章信息
/// </summary>
public class PostInfoModel
{
    /// <summary>
    /// 文章 Id
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 文章标题
    /// </summary>
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 文章总结
    /// </summary>
    [Required]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 是否置顶
    /// </summary>
    [Required]
    public bool IsPinned { get; set; } = false;

    /// <summary>
    /// 作者头像
    /// </summary>
    public string? AutherAvatar { get; set; }

    /// <summary>
    /// 作者名称
    /// </summary>
    public string? AutherName { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [Required]
    public DateTimeOffset Time { get; set; } = DateTimeOffset.UtcNow;

    internal static PostInfoModel FromPost(Post post)
        => new()
        {
            Id = post.Id,
            Title = post.Title,
            Summary = post.Summary,
            IsPinned = post.IsPinned,
            Time = post.UpdateTimeUTC,
            AutherAvatar = post.Auther?.AvatarUrl,
            AutherName = post.Auther?.UserName,
        };
}