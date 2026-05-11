using System.Text.Json.Serialization;
using GZCTF.Models.Data;

namespace GZCTF.Models.Request.Admin;

/// <summary>
/// Flag-egress hit broadcast for the admin live feed.
/// Emitted when a team's dynamic flag is observed in proxied container traffic.
/// </summary>
public class FlagEgressHitModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("gameId")]
    public int GameId { get; set; }

    [JsonPropertyName("participationId")]
    public int ParticipationId { get; set; }

    [JsonPropertyName("challengeId")]
    public int ChallengeId { get; set; }

    [JsonPropertyName("containerId")]
    public Guid? ContainerId { get; set; }

    [JsonPropertyName("teamName")]
    public string TeamName { get; set; } = string.Empty;

    [JsonPropertyName("challengeTitle")]
    public string ChallengeTitle { get; set; } = string.Empty;

    [JsonPropertyName("remoteIp")]
    public string RemoteIp { get; set; } = string.Empty;

    [JsonPropertyName("remotePort")]
    public int RemotePort { get; set; }

    [JsonPropertyName("hitCount")]
    public int HitCount { get; set; }

    [JsonPropertyName("lastSeenUtc")]
    public DateTimeOffset LastSeenUtc { get; set; }

    [JsonPropertyName("firstSeenUtc")]
    public DateTimeOffset FirstSeenUtc { get; set; }

    [JsonPropertyName("direction")]
    public FlagEgressDirection Direction { get; set; }
}
