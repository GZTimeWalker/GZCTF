using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

public enum FlagEgressDirection
{
    ContainerToTeam = 0,
    TeamToContainer = 1
}

[Index(nameof(GameId), nameof(LastSeenUtc))]
public sealed class FlagEgressEvent
{
    [Key]
    public int Id { get; set; }

    public int GameId { get; set; }

    public int ParticipationId { get; set; }

    public int ChallengeId { get; set; }

    public Guid? ContainerId { get; set; }

    [MaxLength(64)]
    public string RemoteIp { get; set; } = string.Empty;

    public int RemotePort { get; set; }

    public DateTimeOffset FirstSeenUtc { get; set; }

    public DateTimeOffset LastSeenUtc { get; set; }

    public int HitCount { get; set; }

    public FlagEgressDirection Direction { get; set; }

    [JsonIgnore]
    public Game Game { get; set; } = null!;

    [JsonIgnore]
    public Participation Participation { get; set; } = null!;

    [JsonIgnore]
    public GameChallenge Challenge { get; set; } = null!;
}
