using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// One row per login blocked by the per-team-user IP / fingerprint
/// policy (<see cref="GZCTF.Models.Internal.AccountPolicy.RequireUniqueIpPerTeamUser"/>
/// and friends). Lets admins see who's being blocked, by which
/// teammate's session, and clear false positives without disabling
/// the whole policy.
/// </summary>
[Index(nameof(OccurredAtUtc))]
public sealed class AntiCheatBlock
{
    [Key]
    public int Id { get; set; }

    /// <summary>The user who was prevented from signing in.</summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>Snapshot of the user's display name at block time
    /// (cheap denormalization so the admin list doesn't have to join
    /// AspNetUsers).</summary>
    [MaxLength(128)]
    public string? UserName { get; set; }

    /// <summary>The conflicting teammate whose session is using the
    /// same IP or fingerprint right now.</summary>
    public Guid? ConflictUserId { get; set; }

    [MaxLength(128)]
    public string? ConflictUserName { get; set; }

    /// <summary>Which dimension fired the block.</summary>
    [Required]
    public AntiCheatBlockKind Kind { get; set; }

    /// <summary>The IP or fingerprint that matched. Stored verbatim so
    /// the admin can decide whether to whitelist or investigate.</summary>
    [MaxLength(256)]
    public string? ConflictingValue { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public enum AntiCheatBlockKind : byte
{
    Ip = 0,
    Fingerprint = 1
}
