using GZCTF.Models.Data;
using GZCTF.Utils;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Services.Transfer;

/// <summary>
/// Purpose string used by <see cref="IDataProtectionProvider"/> to encrypt
/// the per-watch GitHub access token at rest. Keep stable — changing it
/// invalidates already-stored tokens.
/// </summary>
internal static class RepoWatchProtection
{
    public const string Purpose = "GZCTF.RepoWatch.GitHubToken";
}

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
    IDataProtectionProvider dataProtectionProvider,
    ILogger<RepoWatchService> logger)
    : BackgroundService
{
    static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(30);
    readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(RepoWatchProtection.Purpose);

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

        // Lifted to method scope so the outer catch can sanitize against
        // the plaintext value when decrypting succeeded but a later step
        // surfaced an exception that might echo headers / URLs.
        string? plaintextToken = null;

        try
        {
            if (!GitHubLocator.TryParse(watch.RepoUrl, watch.Ref, watch.Subpath, out var loc, out var parseError) || loc is null)
            {
                sync.ErrorMessage = parseError ?? "Failed to parse repo URL.";
                sync.Failed = 1;
                return;
            }

            if (!string.IsNullOrEmpty(watch.GitHubTokenEncrypted))
            {
                try
                {
                    plaintextToken = _protector.Unprotect(watch.GitHubTokenEncrypted);
                    watch.TokenStatus = TokenStatus.Ok;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "RepoWatchService: failed to decrypt token for watch {WatchId}", watch.Id);
                    sync.ErrorMessage = "Stored access token could not be decrypted (purpose/key changed?).";
                    sync.Failed = 1;
                    watch.TokenStatus = TokenStatus.DecryptFailed;
                    return;
                }
            }
            else
            {
                watch.TokenStatus = TokenStatus.NotConfigured;
            }

            var sha = await loc.GetHeadShaAsync(http, plaintextToken, token);
            if (string.IsNullOrEmpty(sha))
            {
                sync.ErrorMessage = "Could not resolve HEAD commit (rate limit, 404, or bad token?).";
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
            var result = await importer.ImportFromGitHubAsync(loc, plaintextToken, opts, token);

            sync.Imported = result.Imported;
            sync.Updated = result.Updated;
            sync.Skipped = result.Skipped;
            sync.Failed = result.Failed;
            if (result.Messages.Count > 0)
                sync.ErrorMessage = Sanitize(TruncateMessages(result.Messages), plaintextToken);

            watch.LastCommitSha = sha;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RepoWatchService: watch {WatchId} failed", watch.Id);
            sync.ErrorMessage = Sanitize(Truncate(ex.Message, 2000), plaintextToken);
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

    static string Sanitize(string? raw, string? secret)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        if (string.IsNullOrEmpty(secret)) return raw;
        return raw.Replace(secret, "***");
    }
}
