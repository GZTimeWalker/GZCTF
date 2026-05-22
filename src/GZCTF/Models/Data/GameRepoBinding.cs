using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// Global admin registration of a github repository whose tree the
/// platform scans for <c>.gzevent</c> markers. Each <c>.gzevent</c>
/// found becomes one <see cref="Game"/>; every <c>challenge.yaml</c>
/// under that event root is imported via the existing
/// <see cref="GZCTF.Services.Transfer.ChallengeImportService"/>.
///
/// Distinct from per-game <see cref="RepoWatch"/> — that targets one
/// game whose admin already exists. This shape creates games
/// automatically and lives at the platform level.
/// </summary>
[Index(nameof(RepoUrl), IsUnique = true)]
[Index(nameof(NextScanUtc), nameof(Status))]
public sealed class GameRepoBinding
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(Limits.UrlLength)]
    public string RepoUrl { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? Ref { get; set; }

    /// <summary>
    /// GitHub access token, encrypted at rest. Required for private
    /// repos; null for public.
    /// </summary>
    [MaxLength(2048)]
    [JsonIgnore]
    public string? GitHubTokenEncrypted { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Guid CreatedByUserId { get; set; }

    /// <summary>How often the background poller should re-scan this
    /// repo. Clamped to <c>[60, 86400]</c> by both the controller and the
    /// scan service.</summary>
    [Required]
    public int IntervalSeconds { get; set; } = 600;

    /// <summary>Reuses <see cref="RepoWatchStatus"/> — Active polls,
    /// Paused skips.</summary>
    [Required]
    public RepoWatchStatus Status { get; set; } = RepoWatchStatus.Active;

    /// <summary>Earliest UTC instant the background poller will pick
    /// this binding up. Null = run on next tick (used as a "due now"
    /// sentinel for new bindings created with RunImmediately).</summary>
    public DateTimeOffset? NextScanUtc { get; set; }

    public DateTimeOffset? LastScanUtc { get; set; }
    [MaxLength(64)]
    public string? LastCommitSha { get; set; }

    /// <summary>Last scan outcome (free-form summary).</summary>
    [MaxLength(1024)]
    public string? LastScanMessage { get; set; }

    /// <summary>Updated by the background poller on every scan. Lets the
    /// admin UI distinguish "token rotated and is now bad" from
    /// "scan failed for some other reason".</summary>
    [Required]
    public TokenStatus TokenStatus { get; set; } = TokenStatus.NotConfigured;

    /// <summary>
    /// Live progress message updated by the scanner as it walks through
    /// the work — "Fetching tarball", "Importing 3/12: tower-of-babel",
    /// etc. Null when nothing is running. Surfaces on the admin
    /// /admin/repo-bindings card so operators can see what the app is
    /// doing in real time instead of waiting for the next
    /// <see cref="LastScanMessage"/> summary.
    /// </summary>
    [MaxLength(256)]
    public string? CurrentActivity { get; set; }

    /// <summary>
    /// Games discovered from this binding. Each one links back via
    /// <see cref="Game.RepoBindingId"/>.
    /// </summary>
    [JsonIgnore]
    public List<Game> Games { get; set; } = [];
}
