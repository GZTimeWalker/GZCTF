using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CTFServer.Models;
public class GameNotice
{
    [Key]
    [JsonIgnore]
    public int Id { get; set; }

    /// <summary>
    /// 通知标题
    /// </summary>
    [Required]
    public NoticeType Type { get; set; } = NoticeType.Normal;

    /// <summary>
    /// 通知内容
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 发布时间
    /// </summary>
    [Required]
    [JsonPropertyName("time")]
    public DateTimeOffset PublishTimeUTC { get; set; } = DateTimeOffset.UtcNow;
}
