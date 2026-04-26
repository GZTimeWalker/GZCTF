using System.Collections.Concurrent;
using System.Net;
using GZCTF.Storage.Interface;

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
///   - ConcurrentDictionary with GetOrAdd for lock-free lookup + lazy creation
///   - Value-based TryRemove in OnRecorderArchived prevents removing a newer recorder
///   - Lock-free TryAcquire on individual recorders
/// </summary>
public sealed class TrafficRecorderRegistry(
    IBlobStorage storage,
    ILogger<TrafficRecorderRegistry> logger) : IAsyncDisposable
{
    readonly ConcurrentDictionary<Guid, Lazy<TrafficRecorder>> _recorders = new();

    /// <summary>
    /// Acquire a TrafficWriter for the given container.
    /// Uses GetOrAdd to avoid unnecessary recorder creation in race conditions.
    /// When the existing recorder is archiving, TryUpdate atomically replaces it.
    /// </summary>
    public TrafficWriter AcquireWriter(TrafficRecorderDescriptor descriptor)
    {
        var key = descriptor.ContainerId;

        while (true)
        {
            var lazyRecorder = _recorders.GetOrAdd(key, _ => CreateRecorder(key, descriptor));
            var recorder = lazyRecorder.Value;

            if (recorder.TryAcquire())
                return new TrafficWriter(recorder);

            // Recorder is archiving — atomically replace with a new one.
            var replacement = CreateRecorder(key, descriptor);

            if (_recorders.TryUpdate(key, replacement, lazyRecorder))
            {
                var replacementRecorder = replacement.Value;
                replacementRecorder.TryAcquire();
                return new TrafficWriter(replacementRecorder);
            }
        }
    }

    /// <summary>
    /// Force-archive the recorder for a specific container.
    /// Called when the container is destroyed.
    /// </summary>
    public async ValueTask ArchiveAsync(Guid containerId)
    {
        if (_recorders.TryRemove(containerId, out var recorder) && recorder.IsValueCreated)
            await recorder.Value.ArchiveAsync();
    }

    /// <summary>
    /// Archive all active recorders. Called on server shutdown via DI disposal.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        var tasks = _recorders.Values
            .Where(r => r.IsValueCreated)
            .Select(r => r.Value.ArchiveAsync().AsTask());
        await Task.WhenAll(tasks);
        _recorders.Clear();
    }

    Lazy<TrafficRecorder> CreateRecorder(Guid key, TrafficRecorderDescriptor descriptor) =>
        new(() => new TrafficRecorder(
            registryKey: key,
            blobPath: BuildBlobPath(descriptor),
            metadata: descriptor.Metadata,
            firstClient: descriptor.ClientEndpoint,
            storage: storage,
            logger: logger,
            onArchived: OnRecorderArchived));

    void OnRecorderArchived(Guid key, TrafficRecorder recorder)
    {
        if (_recorders.TryGetValue(key, out var current) &&
            current.IsValueCreated &&
            ReferenceEquals(current.Value, recorder))
            _recorders.TryRemove(new KeyValuePair<Guid, Lazy<TrafficRecorder>>(key, current));
    }

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
