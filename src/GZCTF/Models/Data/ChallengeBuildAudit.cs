using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// One row per challenge image-build attempt produced by
/// <see cref="GZCTF.Services.Container.Build.ChallengeBuildQueueService"/>.
/// Append-only audit log so retries, transient failures, and bulk
/// rebuilds are all visible after the fact — the per-row
/// <see cref="Challenge.LastBuildLog"/> only carries the latest attempt
/// and gets overwritten on every rebuild.
/// </summary>
[Index(nameof(ChallengeId), nameof(EnqueuedAtUtc), AllDescending = false)]
[Index(nameof(Status), nameof(EnqueuedAtUtc))]
public sealed class ChallengeBuildAudit
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ChallengeId { get; set; }

    [JsonIgnore]
    public GameChallenge Challenge { get; set; } = null!;

    /// <summary>
    /// Game id snapshot — denormalized for fast filtering on
    /// <c>/admin/builds?gameId=…</c> without needing to traverse the
    /// challenge row. Kept in sync at enqueue time.
    /// </summary>
    public int GameId { get; set; }

    public DateTimeOffset EnqueuedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? FinishedAtUtc { get; set; }

    public BuildTrigger Trigger { get; set; }

    /// <summary>
    /// 1-based attempt counter for this build sequence. Auto-retry
    /// fills in 2 / 3 with the same source <see cref="Trigger"/>.
    /// </summary>
    public int Attempt { get; set; } = 1;

    /// <summary>
    /// Terminal status for this audit row. While the worker is running,
    /// the row exists with <see cref="ChallengeBuildStatus.Building"/>;
    /// it is updated to Success/Failed before the worker moves on.
    /// </summary>
    public ChallengeBuildStatus Status { get; set; }

    [MaxLength(128)]
    public string? Digest { get; set; }

    [MaxLength(32768)]
    public string? LogTail { get; set; }

    [MaxLength(512)]
    public string? ErrorMessage { get; set; }

    public long DurationMs { get; set; }
}
