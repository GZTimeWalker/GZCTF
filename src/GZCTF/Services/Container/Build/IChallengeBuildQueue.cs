using System.Collections.Concurrent;

namespace GZCTF.Services.Container.Build;

/// <summary>
/// Snapshot of an in-flight build, exposed to the
/// <c>GET /api/Admin/Builds/InProgress</c> endpoint so the
/// <c>/admin/builds</c> page can show a live strip with elapsed times.
/// </summary>
public sealed record BuildInProgress(
    int AuditId,
    int ChallengeId,
    int GameId,
    string Slug,
    int Attempt,
    BuildTrigger Trigger,
    DateTimeOffset StartedAtUtc);

/// <summary>
/// Outcome of an enqueue attempt.
/// </summary>
public enum EnqueueResult
{
    /// <summary>The job was accepted and will be picked up by a worker.</summary>
    Enqueued,
    /// <summary>A job for this challenge is already queued or currently
    /// building. The caller's intent is considered satisfied — the
    /// existing job will produce the rebuild they wanted.</summary>
    AlreadyPending,
    /// <summary>The bounded channel is full. Operator error or runaway
    /// loop — caller should surface a clear "queue full" message.</summary>
    Rejected
}

/// <summary>
/// Producer-side handle on the challenge image build pipeline. Callers
/// enqueue a job and return immediately; the work happens on a
/// dedicated <see cref="ChallengeBuildQueueService"/> background loop.
///
/// <para>The interface deliberately exposes the in-progress map too,
/// because the admin observability surface needs both "queued/done"
/// (which is in the audit table) and "currently working on" (which is
/// only in worker memory). Implementations must be safe for concurrent
/// reads of <see cref="GetInProgress"/> while writes happen on workers.
/// </para>
///
/// <para><b>Dedup:</b> <see cref="Enqueue"/> checks whether the same
/// <c>ChallengeId</c> already has a job either sitting in the channel
/// or being executed by a worker. If so, the call returns
/// <see cref="EnqueueResult.AlreadyPending"/> and does NOT write a
/// second job. This is what prevents a double-click on the Build
/// button from producing two audit rows and two docker builds.</para>
/// </summary>
public interface IChallengeBuildQueue
{
    /// <summary>
    /// Hand a job to the worker channel. See the dedup note on the
    /// interface — duplicate jobs are silently absorbed.
    /// </summary>
    EnqueueResult Enqueue(ChallengeBuildJob job);

    /// <summary>
    /// True when a job for this challenge is queued or running. Lets
    /// callers (e.g. the per-challenge Rebuild endpoint) avoid setting
    /// up state for a build that won't actually be enqueued.
    /// </summary>
    bool IsPending(int challengeId);

    /// <summary>
    /// Snapshot of every build currently being executed by a worker.
    /// Used by the live-strip endpoint; refresh cadence is the caller's
    /// problem.
    /// </summary>
    IReadOnlyCollection<BuildInProgress> GetInProgress();
}

/// <summary>
/// Default in-memory implementation. Singleton: the channel and
/// in-progress map live for the process lifetime.
/// </summary>
public sealed class ChallengeBuildQueue : IChallengeBuildQueue
{
    private readonly System.Threading.Channels.ChannelWriter<ChallengeBuildJob> _writer;
    private readonly ConcurrentDictionary<int, BuildInProgress> _inProgress = new();

    /// <summary>
    /// Tracks challenges with an enqueued OR running build. Used for
    /// dedup at <see cref="Enqueue"/> time. A challenge enters this
    /// set the moment a job is accepted and leaves when the worker
    /// calls <see cref="MarkEnd"/> in its finally block. AutoRetry
    /// re-enqueues by the worker itself stay in the set across the
    /// backoff delay — preserving the dedup property across retries.
    /// </summary>
    private readonly ConcurrentDictionary<int, byte> _queuedOrRunning = new();

    public ChallengeBuildQueue(System.Threading.Channels.ChannelWriter<ChallengeBuildJob> writer)
    {
        _writer = writer;
    }

    public EnqueueResult Enqueue(ChallengeBuildJob job)
    {
        // TryAdd returns false if the key is already present — that's
        // our dedup signal. We don't write to the channel in that case
        // because some other request already did, and the worker will
        // satisfy both intents with a single docker build.
        if (!_queuedOrRunning.TryAdd(job.ChallengeId, 0))
            return EnqueueResult.AlreadyPending;

        if (!_writer.TryWrite(job))
        {
            // Bounded channel rejected the write. Roll back the dedup
            // entry so a later, less-loaded call can succeed.
            _queuedOrRunning.TryRemove(job.ChallengeId, out _);
            return EnqueueResult.Rejected;
        }

        return EnqueueResult.Enqueued;
    }

    public bool IsPending(int challengeId) => _queuedOrRunning.ContainsKey(challengeId);

    public IReadOnlyCollection<BuildInProgress> GetInProgress() => _inProgress.Values.ToArray();

    internal void MarkStart(BuildInProgress entry) => _inProgress[entry.ChallengeId] = entry;

    internal void MarkEnd(int challengeId)
    {
        _inProgress.TryRemove(challengeId, out _);
        _queuedOrRunning.TryRemove(challengeId, out _);
    }

    /// <summary>
    /// Worker-only path for re-enqueueing a transient-failure retry.
    /// Bypasses the dedup check (the challenge is already in the set
    /// from the original enqueue and stays in across the backoff
    /// delay), but still writes to the bounded channel. If the channel
    /// has somehow filled in the meantime, the retry is lost — the
    /// caller is responsible for surfacing this to the audit row.
    /// </summary>
    internal bool TryRetry(ChallengeBuildJob job) => _writer.TryWrite(job);

    /// <summary>
    /// Worker-only path called at the end of an attempt that did NOT
    /// terminate the build (transient failure → about to retry).
    /// Clears the in-progress entry but leaves the dedup set sticky.
    /// </summary>
    internal void MarkAttemptDoneRetrying(int challengeId)
        => _inProgress.TryRemove(challengeId, out _);
}
