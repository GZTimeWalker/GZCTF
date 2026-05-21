using System.ComponentModel.DataAnnotations;
using GZCTF.Utils;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// Body for <c>POST /api/Edit/Games/{id}/Challenges/ImportFromGitHub</c>.
/// </summary>
public sealed class ImportFromGitHubModel
{
    [Required]
    [MaxLength(Limits.UrlLength)]
    public string RepoUrl { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? Ref { get; set; }

    [MaxLength(512)]
    public string? Subpath { get; set; }
}

/// <summary>
/// Body for <c>POST /api/Edit/Games/{id}/Watches</c>.
/// </summary>
public sealed class RepoWatchCreateModel
{
    [Required]
    [MaxLength(Limits.UrlLength)]
    public string RepoUrl { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? Ref { get; set; }

    [MaxLength(512)]
    public string? Subpath { get; set; }

    /// <summary>
    /// Polling interval, clamped to <c>[60, 86400]</c> by the API.
    /// </summary>
    [Range(60, 86400)]
    public int IntervalSeconds { get; set; } = 600;

    /// <summary>
    /// When true, schedules <c>NextRunUtc = now</c> so the watcher
    /// picks it up on the next tick.
    /// </summary>
    public bool RunImmediately { get; set; } = true;
}

/// <summary>
/// Body for <c>PUT /api/Edit/Games/{id}/Watches/{watchId}</c>. All fields
/// optional; null means "leave alone".
/// </summary>
public sealed class RepoWatchUpdateModel
{
    [MaxLength(128)]
    public string? Ref { get; set; }

    [MaxLength(512)]
    public string? Subpath { get; set; }

    [Range(60, 86400)]
    public int? IntervalSeconds { get; set; }

    public RepoWatchStatus? Status { get; set; }
}

/// <summary>
/// Body for <c>POST .../Reject</c>. Admin-supplied free-form note.
/// </summary>
public sealed class RejectChallengeModel
{
    [MaxLength(Limits.MaxUserDataLength)]
    public string? Note { get; set; }
}
