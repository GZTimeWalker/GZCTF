using GZCTF.Utils;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// Read-only context returned by
/// <c>GET /api/Edit/Games/{id}/WatchBinding</c> when the game was
/// auto-spawned by a global <see cref="GZCTF.Models.Data.GameRepoBinding"/>.
/// The frontend uses it to render the watches page in a
/// "managed by binding" mode instead of the regular per-game
/// watch-create form. No per-game <see cref="GZCTF.Models.Data.RepoWatch"/>
/// row exists for these games — the binding poller is the single
/// authority — so this DTO is what surfaces "the watcher" to the
/// admin.
/// </summary>
public sealed class GameWatchBindingModel
{
    public int BindingId { get; set; }
    public string RepoUrl { get; set; } = string.Empty;
    public string? Ref { get; set; }
    public string? EventManifestPath { get; set; }
    public int IntervalSeconds { get; set; }
    public RepoWatchStatus Status { get; set; }
    public DateTimeOffset? LastScanUtc { get; set; }
    public DateTimeOffset? NextScanUtc { get; set; }
    public string? LastScanMessage { get; set; }
}
