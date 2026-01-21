using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// Event administrator
/// </summary>
[Index(nameof(UserId), nameof(GameId), IsUnique = true)]
public class EventManager
{
    [Key]
    [JsonIgnore]
    public int Id { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// User
    /// </summary>
    public UserInfo User { get; set; } = null!;

    /// <summary>
    /// Game ID
    /// </summary>
    [Required]
    public int GameId { get; set; }

    /// <summary>
    /// Game
    /// </summary>
    public Game Game { get; set; } = null!;
}
