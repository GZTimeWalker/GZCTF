using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// Persisted "watch this github repo for this game" configuration. The
/// <see cref="GZCTF.Services.Transfer.RepoWatchService"/> hosted service
/// scans for due watches every 30s and runs the challenge-import pipeline
/// when the upstream HEAD commit moves.
/// </summary>
[Index(nameof(NextRunUtc), nameof(Status))]
[Index(nameof(GameId))]
public sealed class RepoWatch
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int GameId { get; set; }

    [JsonIgnore]
    public Game Game { get; set; } = null!;

    /// <summary>
    /// Canonical form: <c>https://github.com/{owner}/{repo}</c>. URL parsing
    /// extracts owner+repo+ref+subpath before persistence; this field is
    /// stored exactly as the operator typed it for round-tripping.
    /// </summary>
    [Required]
    [MaxLength(Limits.UrlLength)]
    public string RepoUrl { get; set; } = string.Empty;

    /// <summary>
    /// Optional branch / tag / commit. Null = repo's default branch.
    /// </summary>
    [MaxLength(128)]
    public string? Ref { get; set; }

    /// <summary>
    /// Optional subpath under the repo root. Useful for multi-event repos
    /// where one repo contains <c>quals/</c> and <c>finals/</c> trees.
    /// </summary>
    [MaxLength(512)]
    public string? Subpath { get; set; }

    /// <summary>
    /// Poll interval in seconds. Clamped to <c>[60, 86400]</c> by the API.
    /// </summary>
    [Required]
    public int IntervalSeconds { get; set; } = 600;

    [Required]
    public RepoWatchStatus Status { get; set; } = RepoWatchStatus.Active;

    public DateTimeOffset? NextRunUtc { get; set; }

    public DateTimeOffset? LastRunUtc { get; set; }

    /// <summary>
    /// SHA recorded after the last successful sync. The watcher compares
    /// this against the current HEAD before doing any work — unchanged
    /// shas short-circuit out.
    /// </summary>
    [MaxLength(64)]
    public string? LastCommitSha { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Guid CreatedByUserId { get; set; }

    [JsonIgnore]
    public List<RepoWatchSync> Syncs { get; set; } = [];
}
