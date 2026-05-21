using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// One row per <see cref="GZCTF.Services.Transfer.RepoBindingDiscoveryService"/>
/// scan attempt, recorded so admins can see *which* manifest failed
/// instead of just the truncated summary in
/// <see cref="GameRepoBinding.LastScanMessage"/>.
///
/// Mirror of <see cref="RepoWatchSync"/> for the per-game watch layer.
/// Append-only; the binding poller writes one of these every tick.
/// </summary>
[Index(nameof(BindingId), nameof(RanAtUtc))]
public sealed class GameRepoBindingScan
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int BindingId { get; set; }

    [JsonIgnore]
    public GameRepoBinding Binding { get; set; } = null!;

    public DateTimeOffset RanAtUtc { get; set; } = DateTimeOffset.UtcNow;

    [MaxLength(64)]
    public string? CommitSha { get; set; }

    public int GamesCreated { get; set; }
    public int GamesUpdated { get; set; }
    public int ChallengesImported { get; set; }
    public int ChallengesUpdated { get; set; }
    public int Failures { get; set; }

    /// <summary>
    /// Newline-separated diagnostic messages. Captures per-manifest
    /// failures verbatim (already sanitized of any plaintext PAT by
    /// <see cref="GZCTF.Services.Transfer.RepoBindingDiscoveryService"/>).
    /// </summary>
    [MaxLength(32768)]
    public string? Messages { get; set; }
}
