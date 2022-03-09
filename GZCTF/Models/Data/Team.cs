using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CTFServer.Models;

public class Team
{
    [Key]
    [JsonIgnore]
    public int Id { get; set; }

    /// <summary>
    /// 队伍名称
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 队伍口号
    /// </summary>
    [Required]
    public string Bio { get; set; } = string.Empty;

    /// <summary>
    /// 头像ID
    /// </summary>
    [JsonIgnore]
    public string AvatarId { get; set; } = string.Empty;
}
