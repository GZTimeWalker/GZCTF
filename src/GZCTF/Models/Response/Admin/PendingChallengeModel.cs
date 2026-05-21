using GZCTF.Utils;

namespace GZCTF.Models.Response.Admin;

/// <summary>
/// Row returned by <c>GET /api/Edit/Games/{id}/PendingChallenges</c>.
/// Compact summary for the admin review queue.
/// </summary>
public sealed class PendingChallengeModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public ChallengeCategory Category { get; set; }
    public ChallengeType Type { get; set; }
    public DateTimeOffset? SubmittedAtUtc { get; set; }
    public Guid? SubmittedByUserId { get; set; }
    public string? SubmittedByUserName { get; set; }
}

/// <summary>
/// Row returned by <c>GET /api/Edit/Games/{id}/Watches</c>. Joins
/// <see cref="Models.Data.RepoWatch"/> with the most recent
/// <see cref="Models.Data.RepoWatchSync"/> for a one-shot admin view.
/// </summary>
public sealed class RepoWatchInfoModel
{
    public int Id { get; set; }
    public string RepoUrl { get; set; } = string.Empty;
    public string? Ref { get; set; }
    public string? Subpath { get; set; }
    public int IntervalSeconds { get; set; }
    public RepoWatchStatus Status { get; set; }
    public DateTimeOffset? NextRunUtc { get; set; }
    public DateTimeOffset? LastRunUtc { get; set; }
    public string? LastCommitSha { get; set; }
    public RepoWatchSyncModel? LastSync { get; set; }

    /// <summary>
    /// True iff an encrypted GitHub token is stored on this watch. The
    /// plaintext is never returned by this DTO.
    /// </summary>
    public bool HasGitHubToken { get; set; }
}

/// <summary>
/// One sync attempt as recorded in <see cref="Models.Data.RepoWatchSync"/>.
/// </summary>
public sealed class RepoWatchSyncModel
{
    public DateTimeOffset RanAtUtc { get; set; }
    public string? CommitSha { get; set; }
    public int Imported { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public string? ErrorMessage { get; set; }
}
