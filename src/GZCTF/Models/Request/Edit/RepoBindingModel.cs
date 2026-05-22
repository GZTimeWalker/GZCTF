using System.ComponentModel.DataAnnotations;
using GZCTF.Utils;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// Body for <c>POST /api/Admin/RepoBindings</c>. Persisted as a
/// <see cref="GZCTF.Models.Data.GameRepoBinding"/> and, when
/// <see cref="RunImmediately"/> is true (default), scheduled for an
/// immediate first scan by the background poller.
/// </summary>
public sealed class RepoBindingCreateModel
{
    [Required]
    [MaxLength(Limits.UrlLength)]
    public string RepoUrl { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? Ref { get; set; }

    /// <summary>
    /// Optional GitHub access token (private repos). Encrypted at rest.
    /// </summary>
    [MaxLength(1024)]
    public string? GitHubToken { get; set; }

    /// <summary>Background poll cadence in seconds. Clamped to [60, 86400].</summary>
    [Range(60, 86400)]
    public int IntervalSeconds { get; set; } = 600;

    /// <summary>When true, the binding's first scan happens on the next
    /// poller tick (~30s); otherwise the first scan waits a full
    /// <see cref="IntervalSeconds"/>.</summary>
    public bool RunImmediately { get; set; } = true;
}

/// <summary>
/// Body for <c>PUT /api/Admin/RepoBindings/{id}</c>. Every field is
/// optional with null = leave alone; <see cref="GitHubToken"/> follows
/// the established "" = clear / value = re-protect convention from
/// <see cref="RepoWatchUpdateModel"/>.
/// </summary>
public sealed class RepoBindingUpdateModel
{
    [MaxLength(128)]
    public string? Ref { get; set; }

    [Range(60, 86400)]
    public int? IntervalSeconds { get; set; }

    public GZCTF.Utils.RepoWatchStatus? Status { get; set; }

    [MaxLength(1024)]
    public string? GitHubToken { get; set; }
}

/// <summary>
/// Row returned by <c>GET /api/Admin/RepoBindings</c> with a compact list
/// of child games discovered so far.
/// </summary>
public sealed class RepoBindingInfoModel
{
    public int Id { get; set; }
    public string RepoUrl { get; set; } = string.Empty;
    public string? Ref { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? LastScanUtc { get; set; }
    public DateTimeOffset? NextScanUtc { get; set; }
    public int IntervalSeconds { get; set; }
    public GZCTF.Utils.RepoWatchStatus Status { get; set; }
    public string? LastCommitSha { get; set; }
    public string? LastScanMessage { get; set; }
    public bool HasGitHubToken { get; set; }
    public GZCTF.Utils.TokenStatus TokenStatus { get; set; }

    /// <summary>Live activity from the scanner — non-null while a scan
    /// is actively running.</summary>
    public string? CurrentActivity { get; set; }

    public RepoBindingGameSummary[] Games { get; set; } = [];
}

public sealed class RepoBindingGameSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? EventManifestPath { get; set; }
}

public sealed class RepoBindingScanResultModel
{
    public int GamesCreated { get; set; }
    public int GamesUpdated { get; set; }
    public int ChallengesImported { get; set; }
    public int ChallengesUpdated { get; set; }
    public int Failures { get; set; }
    public string[] Messages { get; set; } = [];
}

/// <summary>Row in the scan-history table for a binding.</summary>
public sealed class RepoBindingScanHistoryModel
{
    public int Id { get; set; }
    public DateTimeOffset RanAtUtc { get; set; }
    public string? CommitSha { get; set; }
    public int GamesCreated { get; set; }
    public int GamesUpdated { get; set; }
    public int ChallengesImported { get; set; }
    public int ChallengesUpdated { get; set; }
    public int Failures { get; set; }
    public string? Messages { get; set; }
}
