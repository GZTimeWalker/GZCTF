using System.Net;
using GZCTF.Models.Data;

namespace GZCTF.Services.Traffic;

/// <summary>
/// A handle for writing traffic packets to a shared TrafficRecorder.
/// Obtained via TrafficRecorderRegistry.AcquireWriter().
/// Disposing this handle decrements the recorder's reference count.
/// </summary>
public sealed class TrafficWriter : IDisposable
{
    readonly TrafficRecorder _recorder;
    readonly FlagEgressService _flagEgress;
    bool _disposed;

    public int Sequence { get; }

    internal TrafficWriter(TrafficRecorder recorder, FlagEgressService flagEgress, int sequence)
    {
        _recorder = recorder;
        _flagEgress = flagEgress;
        Sequence = sequence;
    }

    /// <summary>
    /// Enqueue a traffic packet for pcap writing. Non-blocking.
    /// Silently drops if the recorder is archiving.
    /// </summary>
    public void Write(TrafficPacket packet)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _recorder.Enqueue(packet);
    }

    /// <summary>
    /// Scan a captured buffer for the per-team flag bytes. Synchronous,
    /// SIMD-fast, zero allocation on no-hit and small-packet paths.
    /// </summary>
    public void Inspect(
        ReadOnlySpan<byte> data,
        FlagEgressDirection direction,
        IPEndPoint source,
        IPEndPoint dest,
        DateTimeOffset timestamp)
    {
        if (_disposed) return;
        _flagEgress.Inspect(_recorder.RegistryKey, data, direction, source, dest, timestamp);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _recorder.Release();
    }
}
