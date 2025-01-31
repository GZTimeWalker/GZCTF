using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MemoryPack;

namespace GZCTF.Models.Request.Game;

/// <summary>
/// Basic game information, excluding detailed description and current team registration status
/// </summary>
[MemoryPackable]
public partial class BasicGameInfoModel
{
    [Key]
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Game title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Game summary
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Poster image URL
    /// </summary>
    [JsonPropertyName("poster")]
    public string? PosterUrl => Data.Game.GetPosterUrl(PosterHash);

    /// <summary>
    /// Poster hash
    /// </summary>
    [JsonIgnore]
    public string? PosterHash { get; set; }

    /// <summary>
    /// Team member limit
    /// </summary>
    [JsonPropertyName("limit")]
    public int TeamMemberCountLimit { get; set; }

    /// <summary>
    /// Start time
    /// </summary>
    [Required]
    [JsonPropertyName("start")]
    public DateTimeOffset StartTimeUtc { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    /// <summary>
    /// End time
    /// </summary>
    [Required]
    [JsonPropertyName("end")]
    public DateTimeOffset EndTimeUtc { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    internal static BasicGameInfoModel FromGame(Data.Game game) =>
        new()
        {
            Id = game.Id,
            Title = game.Title,
            Summary = game.Summary,
            PosterHash = game.PosterHash,
            StartTimeUtc = game.StartTimeUtc,
            EndTimeUtc = game.EndTimeUtc,
            TeamMemberCountLimit = game.TeamMemberCountLimit
        };
}
