using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// One row per WebSocket-level proxy connection to a challenge container.
/// Written by <see cref="GZCTF.Services.ContainerAccessLogger"/> on every successful
/// open of <c>/api/proxy/{id}</c>. Powers access-time cheat detection (the
/// connecting user / IP / time is the ground truth that survives encoded
/// flag exfiltration, where plaintext byte matching does not).
/// </summary>
[Index(nameof(GameId), nameof(ConnectedAtUtc))]
[Index(nameof(ChallengeId), nameof(ConnectedAtUtc))]
[Index(nameof(AccessingUserId), nameof(ChallengeId))]
public sealed class ContainerAccessEvent
{
    [Key]
    public int Id { get; set; }

    public int GameId { get; set; }

    public int ChallengeId { get; set; }

    /// <summary>
    /// Participation that owns the container (i.e. the team whose
    /// GameInstance this container belongs to).
    /// </summary>
    public int ContainerOwnerParticipationId { get; set; }

    public Guid ContainerId { get; set; }

    /// <summary>
    /// Authenticated user who opened the proxy WebSocket. Null when the
    /// caller was anonymous (today's controller does not require auth).
    /// </summary>
    public Guid? AccessingUserId { get; set; }

    [MaxLength(256)]
    public string? AccessingUserName { get; set; }

    /// <summary>
    /// The accessing user's own <see cref="Participation"/> in this game,
    /// when resolvable. Compared against
    /// <see cref="ContainerOwnerParticipationId"/> to detect cross-team
    /// proxy access.
    /// </summary>
    public int? AccessingParticipationId { get; set; }

    [MaxLength(64)]
    public string RemoteIp { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? UserAgent { get; set; }

    public DateTimeOffset ConnectedAtUtc { get; set; }

    [JsonIgnore]
    public Game Game { get; set; } = null!;

    [JsonIgnore]
    public GameChallenge Challenge { get; set; } = null!;
}
