using System.ComponentModel.DataAnnotations;
using CTFServer.Models.Request.Edit;
using CTFServer.Utils;

namespace CTFServer.Models;

public class Post
{
    [Key]
    [MaxLength(8)]
    public string Id { get; set; } = Guid.NewGuid().ToString()[..8];

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
    /// 文章内容
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 是否置顶
    /// </summary>
    [Required]
    public bool IsPinned { get; set; } = false;

    /// <summary>
    /// 文章标签
    /// </summary>
    public List<string>? Tags { get; set; } = new();

    /// <summary>
    /// 作者信息
    /// </summary>
    public string? AutherId { get; set; }

    /// <summary>
    /// 作者
    /// </summary>
    public UserInfo? Auther { get; set; }

    /// <summary>
    /// 发布时间
    /// </summary>
    [Required]
    public DateTimeOffset UpdateTimeUTC { get; set; } = DateTimeOffset.UtcNow;

    internal Post Update(PostEditModel model, UserInfo user)
    {
        Title = model.Title;
        Summary = string.IsNullOrEmpty(model.Summary) ? Summary : model.Summary;
        Content = string.IsNullOrEmpty(model.Content) ? Content : model.Content;
        IsPinned = model.IsPinned;
        Tags = model.Tags.Length > 0 ? model.Tags.ToList() : Tags;
        Auther = user;
        AutherId = user.Id;
        UpdateTimeUTC = DateTimeOffset.UtcNow;

        return this;
    }

    internal void UpdateKeyWithHash() => Id = Codec.StrSHA256($"{Title}:{UpdateTimeUTC:s}:{Guid.NewGuid()}")[4..12];
}