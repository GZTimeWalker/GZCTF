using System.Text.Json.Serialization;

namespace GZCTF.Models.Response.Game;

/// <summary>Aggregate like/dislike counts for a single challenge.</summary>
public class ChallengeRatingSummary
{
    [JsonPropertyName("challengeId")]
    public int ChallengeId { get; set; }

    [JsonPropertyName("likes")]
    public int Likes { get; set; }

    [JsonPropertyName("dislikes")]
    public int Dislikes { get; set; }
}
