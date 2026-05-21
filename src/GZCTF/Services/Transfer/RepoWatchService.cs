using GZCTF.Models.Data;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Services.Transfer;

/// <summary>
/// Background service that polls configured <see cref="RepoWatch"/> rows
/// every 30 seconds, fetches the upstream HEAD commit, and triggers a
/// <see cref="ChallengeImportService"/> import when the SHA moves. One
/// <see cref="RepoWatchSync"/> audit row is written per tick regardless
/// of outcome.
///
/// Watches always import with <c>AutoApprove = true</c> — operators
/// chose the repo, so the trust boundary is "admin configured this
/// source", not "any user".
/// </summary>
public sealed class RepoWatchService(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<RepoWatchService> logger)
    : BackgroundService
{
    static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RepoWatchService: started (tick={Interval}s)", TickInterval.TotalSeconds);

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
                logger.LogError(ex, "RepoWatchService: top-level tick error");
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
        var due = await db.RepoWatches
            .Where(w => w.Status == RepoWatchStatus.Active &&
                        (w.NextRunUtc == null || w.NextRunUtc <= now))
            .OrderBy(w => w.NextRunUtc ?? DateTimeOffset.MinValue)
            .Take(8)   // bound work per tick to keep GitHub rate-limit headroom
            .ToListAsync(stoppingToken);

        if (due.Count == 0) return;

        var importer = scope.ServiceProvider.GetRequiredService<ChallengeImportService>();
        var http = httpClientFactory.CreateClient("GitHubApi");

        foreach (var watch in due)
        {
            stoppingToken.ThrowIfCancellationRequested();
            await ProcessWatchAsync(db, importer, http, watch, stoppingToken);
        }
    }

    async Task ProcessWatchAsync(
        AppDbContext db, ChallengeImportService importer, HttpClient http,
        RepoWatch watch, CancellationToken token)
    {
        var sync = new RepoWatchSync { RepoWatchId = watch.Id, RanAtUtc = DateTimeOffset.UtcNow };

        try
        {
            if (!GitHubLocator.TryParse(watch.RepoUrl, watch.Ref, watch.Subpath, out var loc, out var parseError) || loc is null)
            {
                sync.ErrorMessage = parseError ?? "Failed to parse repo URL.";
                sync.Failed = 1;
                return;
            }

            var sha = await loc.GetHeadShaAsync(http, token);
            if (string.IsNullOrEmpty(sha))
            {
                sync.ErrorMessage = "Could not resolve HEAD commit (rate limit or 404?).";
                sync.Failed = 1;
                return;
            }

            sync.CommitSha = sha;

            if (string.Equals(sha, watch.LastCommitSha, StringComparison.Ordinal))
            {
                // No change — record a no-op tick.
                return;
            }

            var opts = new ChallengeImportOptions(watch.GameId, watch.CreatedByUserId, AutoApprove: true);
            var result = await importer.ImportFromGitHubAsync(loc, opts, token);

            sync.Imported = result.Imported;
            sync.Updated = result.Updated;
            sync.Skipped = result.Skipped;
            sync.Failed = result.Failed;
            if (result.Messages.Count > 0)
                sync.ErrorMessage = TruncateMessages(result.Messages);

            watch.LastCommitSha = sha;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RepoWatchService: watch {WatchId} failed", watch.Id);
            sync.ErrorMessage = Truncate(ex.Message, 2000);
            sync.Failed = Math.Max(sync.Failed, 1);
        }
        finally
        {
            // Always advance the schedule so a broken watch doesn't loop hot.
            watch.LastRunUtc = sync.RanAtUtc;
            watch.NextRunUtc = sync.RanAtUtc + TimeSpan.FromSeconds(
                Math.Clamp(watch.IntervalSeconds, 60, 86400));

            db.RepoWatchSyncs.Add(sync);
            await db.SaveChangesAsync(token);
        }
    }

    static string TruncateMessages(IReadOnlyList<string> msgs)
    {
        var joined = string.Join(" | ", msgs);
        return Truncate(joined, 2000);
    }

    static string Truncate(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Length <= max ? s : s[..max];
    }
}
