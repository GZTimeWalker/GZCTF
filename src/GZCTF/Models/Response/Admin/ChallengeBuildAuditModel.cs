using GZCTF.Services.Container.Build;
using GZCTF.Utils;

namespace GZCTF.Models.Response.Admin;

/// <summary>
/// One row of the <c>/admin/builds</c> history table. Read-only
/// projection of <see cref="GZCTF.Models.Data.ChallengeBuildAudit"/>
/// plus a denormalized challenge title for display.
/// </summary>
public sealed class ChallengeBuildAuditModel
{
    public int Id { get; set; }
    public int ChallengeId { get; set; }
    public int GameId { get; set; }
    public string ChallengeTitle { get; set; } = string.Empty;
    public DateTimeOffset EnqueuedAtUtc { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? FinishedAtUtc { get; set; }
    public BuildTrigger Trigger { get; set; }
    public int Attempt { get; set; }
    public ChallengeBuildStatus Status { get; set; }
    public string? Digest { get; set; }
    public string? LogTail { get; set; }
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
}

/// <summary>
/// One row of the live in-progress strip on <c>/admin/builds</c>.
/// </summary>
public sealed class ChallengeBuildInProgressModel
{
    public int AuditId { get; set; }
    public int ChallengeId { get; set; }
    public int GameId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public int Attempt { get; set; }
    public BuildTrigger Trigger { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
}

/// <summary>
/// Result of a "Rebuild all failed" bulk action against a game.
/// </summary>
public sealed class BulkRebuildResultModel
{
    public int Enqueued { get; set; }
    public int Skipped { get; set; }
    public string[] Messages { get; set; } = [];
}
