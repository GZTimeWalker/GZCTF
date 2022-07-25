using CTFServer.Models.Request.Edit;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CTFServer.Models;

public class Notice
{
    [Key]
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// 通知标题
    /// </summary>
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 通知内容
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 是否置顶
    /// </summary>
    [Required]
    public bool IsPinned { get; set; } = false;

    /// <summary>
    /// 发布时间
    /// </summary>
    [Required]
    [JsonPropertyName("time")]
    public DateTimeOffset PublishTimeUTC { get; set; } = DateTimeOffset.UtcNow;

    public void UpdateInfo(NoticeModel model)
    {
        Title = model.Title;
        Content = model.Content;
        IsPinned = model.IsPinned;
        PublishTimeUTC = DateTimeOffset.UtcNow;
    }
}