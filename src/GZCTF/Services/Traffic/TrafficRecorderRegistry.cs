using System.Collections.Concurrent;
using System.Net;
using GZCTF.Storage.Interface;
using GZCTF.Utils;

namespace GZCTF.Services.Traffic;

/// <summary>
/// Describes the container context needed to create/locate a TrafficRecorder.
/// Passed by ProxyController when acquiring a writer.
/// </summary>
public readonly record struct TrafficRecorderDescriptor(
    Guid ContainerId,
    int ChallengeId,
    int ParticipationId,
    string ConnectionId,
    byte[]? Metadata,
    IPEndPoint ClientEndpoint);

/// <summary>
/// Singleton registry managing all active TrafficRecorders.
/// Each container has at most one active recorder per GZCTF instance at a time.
///
/// Thread-safety:
///   - ConcurrentDictionary for recorder lookup
///   - Lock-free TryAcquire on individual recorders
///   - Retry loop handles races between archive and acquire
/// </summary>
public sealed class TrafficRecorderRegistry(
    IBlobStorage storage,
    ILogger<TrafficRecorderRegistry> logger) : IAsyncDisposable
{
    readonly ConcurrentDictionary<Guid, TrafficRecorder> _recorders = new();

    /// <summary>
    /// Acquire a TrafficWriter for the given container.
    /// Creates a new TrafficRecorder if none exists or the existing one is archiving.
    /// </summary>
    public TrafficWriter AcquireWriter(TrafficRecorderDescriptor descriptor)
    {
        var key = descriptor.ContainerId;

        while (true)
        {
            if (_recorders.TryGetValue(key, out var existing))
            {
                if (existing.TryAcquire())
                    return new TrafficWriter(existing);

                if (existing.IsFullyDrained)
                    _recorders.TryRemove(key, out _);
            }

            var blobPath = BuildBlobPath(descriptor);

            var recorder = new TrafficRecorder(
                registryKey: key,
                blobPath: blobPath,
                metadata: descriptor.Metadata,
                firstClient: descriptor.ClientEndpoint,
                storage: storage,
                logger: logger,
                onArchived: OnRecorderArchived);

            if (_recorders.TryAdd(key, recorder))
                return new TrafficWriter(recorder);

            // Race: another thread added one first.
            // Dispose ours (it has no external writers, safe to archive immediately).
            _ = recorder.DisposeAsync();
        }
    }

    /// <summary>
    /// Force-archive the recorder for a specific container.
    /// Called when the container is destroyed.
    /// </summary>
    public async ValueTask ArchiveAsync(Guid containerId)
    {
        if (_recorders.TryRemove(containerId, out var recorder))
            await recorder.ArchiveAsync();
    }

    /// <summary>
    /// Archive all active recorders. Called on server shutdown via DI disposal.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        var tasks = _recorders.Values.Select(r => r.ArchiveAsync().AsTask());
        await Task.WhenAll(tasks);
        _recorders.Clear();
    }

    void OnRecorderArchived(Guid key, TrafficRecorder recorder) =>
        _recorders.TryRemove(key, out _);

    static string BuildBlobPath(TrafficRecorderDescriptor descriptor)
    {
        var shortId = descriptor.ContainerId.ToString("N")[..12];

        return StoragePath.Combine(
            PathHelper.Capture,
            descriptor.ChallengeId.ToString(),
            descriptor.ParticipationId.ToString(),
            $"{shortId}-{descriptor.ConnectionId}.pcap");
    }
}
