using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CTFServer.Models;
public class Announcement
{
    [Key]
    [JsonIgnore]
    public int Id {  get; set; }

    [Required]
    public string Title {  get; set; } = string.Empty;

    [Required]
    public string Content {  get; set; } = string.Empty;

    [Required]
    public bool IsPinned { get; set; } = false;

    [Required]
    [JsonPropertyName("time")]
    public DateTimeOffset PublishTimeUTC { get; set; } = DateTimeOffset.UtcNow;
}
