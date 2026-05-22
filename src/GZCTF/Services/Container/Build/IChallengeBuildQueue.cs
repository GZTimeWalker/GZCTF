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
/// </summary>
public interface IChallengeBuildQueue
{
    /// <summary>
    /// Hands a job to the worker channel. Returns synchronously; the
    /// build may not start for up to a few seconds depending on worker
    /// availability. The audit row is created by the worker, not here,
    /// so callers should set <c>ChallengeBuildStatus.Queued</c> on the
    /// challenge themselves before calling this if they want the UI to
    /// reflect the queue state instantly.
    /// </summary>
    void Enqueue(ChallengeBuildJob job);

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

    public ChallengeBuildQueue(System.Threading.Channels.ChannelWriter<ChallengeBuildJob> writer)
    {
        _writer = writer;
    }

    public void Enqueue(ChallengeBuildJob job)
    {
        if (!_writer.TryWrite(job))
            throw new InvalidOperationException("Challenge build queue is full or closed.");
    }

    public IReadOnlyCollection<BuildInProgress> GetInProgress() => _inProgress.Values.ToArray();

    internal void MarkStart(BuildInProgress entry) => _inProgress[entry.ChallengeId] = entry;

    internal void MarkEnd(int challengeId) => _inProgress.TryRemove(challengeId, out _);
}
