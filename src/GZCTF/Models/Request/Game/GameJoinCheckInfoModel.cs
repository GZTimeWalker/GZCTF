using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GZCTF.Models.Request.Game;

public class GameJoinCheckInfoModel
{
    /// <summary>
    /// The teams that the current user has joined and participated in the game
    /// </summary>
    public JoinedTeam[] JoinedTeams { get; set; } = [];

    /// <summary>
    /// IDs of divisions that can be joined
    /// </summary>
    public HashSet<int> JoinableDivisions { get; set; } = [];
}

public record JoinedTeam
{
    /// <summary>
    /// Team ID
    /// </summary>
    [Required]
    [JsonPropertyName("id")]
    public int TeamId { get; set; }

    /// <summary>
    /// The division ID the team has joined
    /// </summary>
    [Required]
    [JsonPropertyName("division")]
    public int? DivisionId { get; set; }
}
