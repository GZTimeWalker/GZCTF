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
    int GameId,
    string? Flag,
    bool IsStaticFlag,
    byte[]? Metadata,
    string ConnectionId,
    IPAddress? RemoteIpAddress);

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
    ILoggerFactory loggerFactory,
    FlagEgressService flagEgress) : IAsyncDisposable
{
    readonly ConcurrentDictionary<Guid, Lazy<TrafficRecorder>> _recorders = new();

    /// <summary>
    /// Acquire a TrafficWriter for the given container.
    /// Uses GetOrAdd to avoid unnecessary recorder creation in race conditions.
    /// When the existing recorder is archiving, TryUpdate atomically replaces it.
    /// </summary>
    internal TrafficWriter AcquireWriter(TrafficRecorderDescriptor descriptor)
    {
        var key = descriptor.ContainerId;

        while (true)
        {
            var lazyRecorder = _recorders.GetOrAdd(key, _ => CreateRecorder(key, descriptor));
            var recorder = lazyRecorder.Value;

            var seq = recorder.TryAcquire();
            if (seq > 0)
                return new TrafficWriter(recorder, flagEgress, seq);

            // Recorder is archiving — atomically replace with a new one.
            var replacement = CreateRecorder(key, descriptor);

            if (_recorders.TryUpdate(key, replacement, lazyRecorder))
            {
                var newRecorder = replacement.Value;
                var newSeq = newRecorder.TryAcquire();
                if (newSeq > 0)
                    return new TrafficWriter(newRecorder, flagEgress, newSeq);
            }
        }
    }

    /// <summary>
    /// Force-archive the recorder for a specific container.
    /// Called when the container is destroyed.
    /// </summary>
    internal async ValueTask ArchiveAsync(Guid containerId)
    {
        // always try to archive the recorder, to avoid a race condition
        // where the same recorder is acquiring after the `GetOrAdd`
        // in which case this recorder would not clean up
        if (_recorders.TryRemove(containerId, out var recorder))
            await recorder.Value.ArchiveAsync();

        flagEgress.UnregisterRecorder(containerId);
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

    Lazy<TrafficRecorder> CreateRecorder(Guid key, TrafficRecorderDescriptor descriptor)
    {
        // Inspector registration must happen before the recorder is returned so
        // that the first CaptureNetworkStream packet finds an inspector ready.
        // Note: scanning is performed at the CaptureNetworkStream seam (not in
        // the recorder's write loop) so the recorder's metadata packet — which
        // intentionally contains the flag string — never reaches the inspector.
        flagEgress.RegisterRecorder(descriptor);

        return new(() => new TrafficRecorder(
            registryKey: key,
            blobPath: BuildBlobPath(descriptor),
            metadata: descriptor.Metadata,
            remoteAddress: descriptor.RemoteIpAddress?.MapToIPv6() ?? IPAddress.IPv6Loopback,
            storage: storage,
            logger: loggerFactory.CreateLogger<TrafficRecorder>(),
            onArchived: OnRecorderArchived));
    }

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
