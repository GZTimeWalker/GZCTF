namespace GZCTF.Services.Traffic;

/// <summary>
/// A handle for writing traffic packets to a shared TrafficRecorder.
/// Obtained via TrafficRecorderRegistry.AcquireWriter().
/// Disposing this handle decrements the recorder's reference count.
/// </summary>
public sealed class TrafficWriter : IAsyncDisposable
{
    readonly TrafficRecorder _recorder;
    bool _disposed;

    internal TrafficWriter(TrafficRecorder recorder) => _recorder = recorder;

    /// <summary>
    /// Enqueue a traffic packet for pcap writing. Non-blocking.
    /// Silently drops if the recorder is archiving.
    /// </summary>
    public void Write(TrafficPacket packet)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _recorder.Enqueue(packet);
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
            return ValueTask.CompletedTask;

        _disposed = true;
        return _recorder.ReleaseAsync();
    }
}
