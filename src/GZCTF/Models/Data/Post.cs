using System.ComponentModel.DataAnnotations;
using GZCTF.Models.Request.Edit;
using MemoryPack;

namespace GZCTF.Models.Data;

[MemoryPackable]
public partial class Post
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
    public bool IsPinned { get; set; }

    /// <summary>
    /// 文章标签
    /// </summary>
    public List<string>? Tags { get; set; } = new();

    /// <summary>
    /// 作者信息
    /// </summary>
    public string? AuthorId { get; set; }

    /// <summary>
    /// 作者
    /// </summary>
    public UserInfo? Author { get; set; }

    /// <summary>
    /// 发布时间
    /// </summary>
    [Required]
    public DateTimeOffset UpdateTimeUTC { get; set; } = DateTimeOffset.UtcNow;

    internal Post Update(PostEditModel model, UserInfo user)
    {
        Title = model.Title;
        Summary = model.Summary;
        Content = model.Content;
        IsPinned = model.IsPinned;
        Tags = model.Tags.ToList();
        Author = user;
        AuthorId = user.Id;
        UpdateTimeUTC = DateTimeOffset.UtcNow;

        return this;
    }

    internal void UpdateKeyWithHash() => Id = $"{Title}:{UpdateTimeUTC:s}:{Guid.NewGuid()}".StrSHA256()[4..12];
}