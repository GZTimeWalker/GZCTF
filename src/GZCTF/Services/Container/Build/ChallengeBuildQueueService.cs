using System.Diagnostics;
using System.Threading.Channels;
using GZCTF.Models;
using GZCTF.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Services.Container.Build;

/// <summary>
/// Background service that drains the
/// <see cref="ChallengeBuildJob"/> channel using a small fixed worker
/// pool, calls <see cref="IChallengeImageBuilder.BuildAsync"/>, and
/// persists the outcome to the <see cref="GameChallenge"/> row plus an
/// append-only <see cref="ChallengeBuildAudit"/> entry.
///
/// <para>Design constraints:</para>
/// <list type="bullet">
///   <item>Workers must never share a DbContext — each iteration opens
///   its own scope, same pattern as the repo-binding poller.
///   </item>
///   <item>Build attempts must be visible: the
///   <c>ChallengeBuildStatus.Building</c> state is persisted as soon as
///   a worker picks up the job, and the audit row is created up-front
///   so the live strip and history table both have something to render.
///   </item>
///   <item>Transient failures (daemon refused, 5xx, EOF mid-build) get
///   up to 3 retries with 10s / 30s / 90s backoff. Non-transient errors
///   (Dockerfile syntax, missing base image, unauthorized) fail
///   immediately on attempt 1 to avoid pointless retry storms.</item>
///   <item>On host restart any row still flagged
///   <see cref="ChallengeBuildStatus.Building"/> is reset to
///   <see cref="ChallengeBuildStatus.Failed"/> with a clear message so
///   nothing stays "stuck" forever after a crash.</item>
/// </list>
/// </summary>
public sealed class ChallengeBuildQueueService(
    ChannelReader<ChallengeBuildJob> reader,
    IChallengeBuildQueue queue,
    IChallengeImageBuilder imageBuilder,
    IServiceScopeFactory scopeFactory,
    ILogger<ChallengeBuildQueueService> logger) : BackgroundService
{
    private const int WorkerCount = 2;
    private const int MaxAttempts = 3;
    private static readonly TimeSpan[] BackoffSchedule =
    {
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(90)
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ResetStuckBuildsAsync(stoppingToken);
        SweepOrphanedSnapshots();

        logger.LogInformation("ChallengeBuildQueueService: starting {Workers} workers", WorkerCount);

        var workers = new Task[WorkerCount];
        for (int i = 0; i < WorkerCount; i++)
        {
            int idx = i;
            workers[i] = Task.Run(() => WorkerLoop(idx, stoppingToken), stoppingToken);
        }

        await Task.WhenAll(workers);
    }

    /// <summary>
    /// Flips any leftover Building rows from a previous process
    /// lifetime to Failed. Without this, an admin who restarted the app
    /// mid-build is stuck staring at a yellow "Building" badge that
    /// will never resolve. Mirrors the priming pattern in
    /// <see cref="FlagChecker.StartAsync"/>.
    /// </summary>
    async Task ResetStuckBuildsAsync(CancellationToken token)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stuck = await db.GameChallenges
                .Where(c => c.BuildStatus == ChallengeBuildStatus.Building
                            || c.BuildStatus == ChallengeBuildStatus.Queued)
                .ToListAsync(token);
            if (stuck.Count == 0) return;

            var now = DateTimeOffset.UtcNow;
            foreach (var ch in stuck)
            {
                ch.BuildStatus = ChallengeBuildStatus.Failed;
                ch.LastBuildLog = "Build interrupted by app restart.";
                db.ChallengeBuildAudits.Add(new ChallengeBuildAudit
                {
                    ChallengeId = ch.Id,
                    GameId = ch.GameId,
                    EnqueuedAtUtc = now,
                    StartedAtUtc = now,
                    FinishedAtUtc = now,
                    Trigger = BuildTrigger.AutoRetry,
                    Attempt = 1,
                    Status = ChallengeBuildStatus.Failed,
                    ErrorMessage = "Interrupted by app restart",
                    LogTail = ch.LastBuildLog,
                    DurationMs = 0
                });
            }
            await db.SaveChangesAsync(token);
            logger.LogWarning("ChallengeBuildQueueService: reset {Count} stuck builds", stuck.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ChallengeBuildQueueService: failed to reset stuck builds");
        }
    }

    /// <summary>
    /// Sweep <c>/tmp/gzctf-build-*</c> dirs older than 1 hour at
    /// startup. Snapshot dirs leak when the worker process is killed
    /// between <c>PrepareBuildSnapshot</c> and the cleanup
    /// <c>finally</c>. 1 hour is well past the 5-minute docker build
    /// timeout, so anything older than that is definitely orphaned.
    /// </summary>
    void SweepOrphanedSnapshots()
    {
        try
        {
            var threshold = DateTime.UtcNow - TimeSpan.FromHours(1);
            int swept = 0;
            foreach (var dir in Directory.EnumerateDirectories(Path.GetTempPath(), "gzctf-build-*"))
            {
                try
                {
                    var info = new DirectoryInfo(dir);
                    if (info.LastWriteTimeUtc < threshold)
                    {
                        Directory.Delete(dir, recursive: true);
                        swept++;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "ChallengeBuildQueueService: could not sweep {Dir}", dir);
                }
            }
            if (swept > 0)
                logger.LogInformation("ChallengeBuildQueueService: swept {Count} orphan snapshot dirs", swept);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ChallengeBuildQueueService: snapshot sweep failed");
        }
    }

    async Task WorkerLoop(int workerId, CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var job in reader.ReadAllAsync(stoppingToken))
            {
                try { await ProcessOneAsync(workerId, job, stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "ChallengeBuildQueueService: worker {W} crashed on challenge {Id} attempt {A}",
                        workerId, job.ChallengeId, job.Attempt);
                    // Best-effort cleanup so we don't leak temp dirs on
                    // a runaway exception inside ProcessOneAsync.
                    if (job.OwnsContextDir) SafeDelete(job.ContextDir);
                }
            }
        }
        catch (OperationCanceledException) { /* expected on shutdown */ }
    }

    async Task ProcessOneAsync(int workerId, ChallengeBuildJob job, CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var startedAt = DateTimeOffset.UtcNow;
        ChallengeBuildAudit audit;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ch = await db.GameChallenges.FirstOrDefaultAsync(c => c.Id == job.ChallengeId, stoppingToken);
            if (ch is null)
            {
                logger.LogWarning(
                    "ChallengeBuildQueueService: challenge {Id} disappeared before build (worker {W})",
                    job.ChallengeId, workerId);
                if (job.OwnsContextDir) SafeDelete(job.ContextDir);
                return;
            }

            ch.BuildStatus = ChallengeBuildStatus.Building;
            audit = new ChallengeBuildAudit
            {
                ChallengeId = ch.Id,
                GameId = ch.GameId,
                EnqueuedAtUtc = startedAt,
                StartedAtUtc = startedAt,
                Trigger = job.Trigger,
                Attempt = job.Attempt,
                Status = ChallengeBuildStatus.Building
            };
            db.ChallengeBuildAudits.Add(audit);
            await db.SaveChangesAsync(stoppingToken);
        }

        var inflight = new BuildInProgress(audit.Id, job.ChallengeId, job.GameId, job.Slug,
            job.Attempt, job.Trigger, startedAt);
        ((ChallengeBuildQueue)queue).MarkStart(inflight);

        // Live-log sink. Each docker output line is appended to a
        // local buffer; every ~2 seconds (FlushIntervalMs) we push the
        // current tail to Challenge.LastBuildLog so the admin UI — which
        // polls AuditMeta every 2s while the status is Building — can
        // render the log as it streams. Without this the modal sits on
        // an empty Code block until the build completes, which feels
        // broken for builds that take a minute or more.
        var liveBuf = new System.Text.StringBuilder();
        var liveLock = new object();
        long lastFlushTicks = 0;
        const long FlushIntervalMs = 2000;

        Action<string> sink = line =>
        {
            // Scrub before appending so the live UI never briefly
            // shows an unscrubbed token between two flushes.
            line = DockerChallengeImageBuilder.ScrubSecrets(line);
            lock (liveLock)
            {
                liveBuf.Append(line);
                if (liveBuf.Length > 32 * 1024)
                    liveBuf.Remove(0, liveBuf.Length - 32 * 1024);
            }
            var now = Environment.TickCount64;
            if (now - Interlocked.Read(ref lastFlushTicks) < FlushIntervalMs) return;
            Interlocked.Exchange(ref lastFlushTicks, now);
            string snapshot;
            lock (liveLock) { snapshot = liveBuf.ToString(); }
            // Fire-and-forget DB write — losing one progress flush is
            // fine; what matters is that the operator sees something
            // change every couple of seconds.
            _ = Task.Run(async () =>
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await db.GameChallenges
                        .Where(c => c.Id == job.ChallengeId)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastBuildLog, snapshot));
                }
                catch { /* swallow — next flush will retry */ }
            });
        };

        ChallengeBuildResult? result = null;
        Exception? thrown = null;
        try
        {
            result = await imageBuilder.BuildAsync(
                new ChallengeBuildRequest(job.GameId, job.Slug, job.ContextDir, job.Dockerfile),
                stoppingToken,
                sink);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // App is shutting down; leave the audit row in Building so
            // the next startup's ResetStuckBuildsAsync flips it to
            // Failed with the right message.
            ((ChallengeBuildQueue)queue).MarkEnd(job.ChallengeId);
            return;
        }
        catch (Exception ex)
        {
            thrown = ex;
        }
        finally
        {
            stopwatch.Stop();
            // NOTE: we do NOT call MarkEnd here unconditionally
            // anymore. If a transient retry is about to be scheduled,
            // we want the challenge to stay in the dedup set so a
            // concurrent human-triggered Build click doesn't slip in
            // and create a duplicate. MarkEnd happens after the
            // retry decision below.
        }

        var finishedAt = DateTimeOffset.UtcNow;
        bool success = result is { Success: true };
        string errorMessage = thrown?.Message ?? result?.ErrorMessage ?? string.Empty;
        bool transient = !success && IsTransient(errorMessage)
                              && job.Attempt < MaxAttempts
                              && !stoppingToken.IsCancellationRequested;

        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ch = await db.GameChallenges.FirstOrDefaultAsync(c => c.Id == job.ChallengeId, stoppingToken);
            var auditRow = await db.ChallengeBuildAudits.FirstOrDefaultAsync(a => a.Id == audit.Id, stoppingToken);

            string logTail = result?.LogTail ?? thrown?.ToString() ?? string.Empty;
            string truncatedErr = Truncate(errorMessage, 512);

            if (auditRow is not null)
            {
                auditRow.FinishedAtUtc = finishedAt;
                auditRow.DurationMs = stopwatch.ElapsedMilliseconds;
                auditRow.LogTail = Truncate(logTail, 32 * 1024);
                auditRow.Digest = result?.Digest;
                auditRow.ErrorMessage = success ? null : truncatedErr;
                auditRow.Status = success
                    ? ChallengeBuildStatus.Success
                    : (transient ? ChallengeBuildStatus.Building : ChallengeBuildStatus.Failed);
            }

            if (ch is not null)
            {
                ch.LastBuildLog = Truncate(logTail, 32 * 1024);
                if (success)
                {
                    ch.BuildStatus = ChallengeBuildStatus.Success;
                    ch.BuildImageDigest = result?.Digest;
                    if (!string.IsNullOrEmpty(result?.ImageTag))
                        ch.ContainerImage = result.ImageTag;
                }
                else if (!transient)
                {
                    ch.BuildStatus = ChallengeBuildStatus.Failed;
                }
                // transient: leave BuildStatus = Building so the UI
                // doesn't flicker to red before the retry lands.
            }

            await db.SaveChangesAsync(stoppingToken);
        }

        var q = (ChallengeBuildQueue)queue;
        if (transient)
        {
            var delay = BackoffSchedule[Math.Min(job.Attempt - 1, BackoffSchedule.Length - 1)];
            logger.LogWarning(
                "ChallengeBuildQueueService: transient failure on challenge {Id} attempt {A}, retrying in {D}s: {Err}",
                job.ChallengeId, job.Attempt, delay.TotalSeconds, Truncate(errorMessage, 200));
            // Clear the in-progress entry but keep the challenge in the
            // dedup set across the backoff delay (TryRetry bypasses the
            // dedup check on the re-enqueue path).
            q.MarkAttemptDoneRetrying(job.ChallengeId);
            try { await Task.Delay(delay, stoppingToken); }
            catch (OperationCanceledException) { q.MarkEnd(job.ChallengeId); return; }
            if (!q.TryRetry(job with { Attempt = job.Attempt + 1, Trigger = BuildTrigger.AutoRetry }))
            {
                logger.LogError("ChallengeBuildQueueService: failed to re-enqueue retry for {Id}", job.ChallengeId);
                // Channel rejected the retry write (full?). Clear the
                // dedup set so the operator can manually retry.
                q.MarkEnd(job.ChallengeId);
            }
            return;
        }

        // Terminal outcome — clear in-progress AND dedup set so future
        // builds for this challenge enqueue normally.
        q.MarkEnd(job.ChallengeId);
        if (job.OwnsContextDir) SafeDelete(job.ContextDir);
    }

    /// <summary>
    /// Heuristic classifier for "should we retry this?". Errs on the
    /// side of NOT retrying: a Dockerfile-syntax bug looped 3 times
    /// wastes ~2 minutes of operator time and never succeeds, while a
    /// daemon hiccup is rare and obvious. The matched strings cover
    /// the common Docker.DotNet transport-layer failures we've seen.
    /// </summary>
    static bool IsTransient(string err)
    {
        if (string.IsNullOrEmpty(err)) return false;
        var e = err.ToLowerInvariant();
        return e.Contains("cannot connect to the docker daemon")
            || e.Contains("connection refused")
            || e.Contains("connection reset")
            || e.Contains("i/o timeout")
            || e.Contains("eof")
            || e.Contains("temporarily unavailable")
            || e.Contains("503")
            || e.Contains("504")
            || e.Contains("502");
    }

    static string Truncate(string s, int max)
        => string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max];

    static void SafeDelete(string dir)
    {
        try
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
        catch { /* worker should never crash on cleanup */ }
    }
}
