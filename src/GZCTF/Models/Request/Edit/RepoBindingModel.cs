using System.ComponentModel.DataAnnotations;
using GZCTF.Utils;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// Body for <c>POST /api/Admin/RepoBindings</c>. Triggers an immediate
/// scan after persistence.
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
    public string? LastCommitSha { get; set; }
    public string? LastScanMessage { get; set; }
    public bool HasGitHubToken { get; set; }
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
