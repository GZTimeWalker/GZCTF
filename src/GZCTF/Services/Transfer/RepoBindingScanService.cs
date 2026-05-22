using GZCTF.Models.Data;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Services.Transfer;

/// <summary>
/// Background service that polls configured <see cref="GameRepoBinding"/>
/// rows every 30 seconds and calls
/// <see cref="RepoBindingDiscoveryService.ScanAsync"/> on every binding
/// whose <see cref="GameRepoBinding.NextScanUtc"/> has passed.
///
/// Mirror of <see cref="RepoWatchService"/> for the per-game watch
/// layer — same tick cadence, same fault isolation per row, same
/// "always advance NextScanUtc so a broken target doesn't hot-loop"
/// invariant. The discovery itself is idempotent (upsert by
/// <c>(BindingId, EventManifestPath)</c>), so re-running per tick is
/// safe even when nothing changed in the repo.
/// </summary>
public sealed class RepoBindingScanService(
    IServiceScopeFactory scopeFactory,
    ILogger<RepoBindingScanService> logger) : BackgroundService
{
    static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RepoBindingScanService: started (tick={Interval}s)", TickInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RepoBindingScanService: top-level tick error");
            }

            try { await Task.Delay(TickInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    async Task TickAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTimeOffset.UtcNow;

        // Watchdog: clear CurrentActivity on any binding that hasn't
        // completed a scan in 15 minutes. The scanner writes activity
        // updates through SetActivityAsync as it walks the tree, and
        // the finally block sets it back to null. But if the worker
        // process is killed mid-fetch, the field stays set and the UI
        // shows "Downloading tarball…" forever. 15 minutes is far past
        // the 2-minute git timeout + the longest realistic import, so
        // anything still "scanning" after that is definitely stuck.
        var staleThreshold = now - TimeSpan.FromMinutes(15);
        try
        {
            await db.GameRepoBindings
                .Where(b => b.CurrentActivity != null
                            && (b.LastScanUtc == null || b.LastScanUtc < staleThreshold))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.CurrentActivity, (string?)null),
                    stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "RepoBindingScanService: stale-activity watchdog failed");
        }

        // Pull the small projection only — we just need ids + creator
        // to call the discovery scope, and the interval to advance the
        // next-run gate.
        var due = await db.GameRepoBindings
            .AsNoTracking()
            .Where(b => b.Status == RepoWatchStatus.Active &&
                        (b.NextScanUtc == null || b.NextScanUtc <= now))
            .OrderBy(b => b.NextScanUtc ?? DateTimeOffset.MinValue)
            .Take(8) // bound work per tick — discovery touches the github API
            .Select(b => new { b.Id, b.CreatedByUserId, b.IntervalSeconds })
            .ToListAsync(stoppingToken);

        if (due.Count == 0) return;

        var discovery = scope.ServiceProvider.GetRequiredService<RepoBindingDiscoveryService>();

        foreach (var b in due)
        {
            stoppingToken.ThrowIfCancellationRequested();
            try
            {
                await discovery.ScanAsync(b.Id, b.CreatedByUserId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "RepoBindingScanService: scan failed for binding {Id}", b.Id);
            }

            // Always advance NextScanUtc — even on failure — so a broken
            // repo can't keep the loop hot. The discovery service has
            // already written LastScanMessage with the diagnosis.
            var clamped = Math.Clamp(b.IntervalSeconds, 60, 86400);
            var next = DateTimeOffset.UtcNow.AddSeconds(clamped);
            try
            {
                await db.GameRepoBindings
                    .Where(x => x.Id == b.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.NextScanUtc, next), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "RepoBindingScanService: failed to advance NextScanUtc for {Id}", b.Id);
            }
        }
    }
}
