using System.Net;
using System.Net.Sockets;
using GZCTF.Services.Traffic;

namespace GZCTF.Utils;

/// <summary>
/// A NetworkStream that intercepts reads/writes and forwards captured traffic
/// to a TrafficWriter for aggregated pcap recording.
///
/// Replaces RecordableNetworkStream. This class is only responsible for
/// interception and packet creation — pcap writing is delegated to the
/// TrafficRecorder via the TrafficWriter handle.
/// </summary>
public sealed class CaptureNetworkStream : NetworkStream
{
    readonly TrafficWriter? _writer;
    readonly IPEndPoint _source;
    readonly IPEndPoint _dest;
    bool _disposed;

    public CaptureNetworkStream(
        Socket socket,
        TrafficWriter? writer,
        IPEndPoint source,
        IPEndPoint dest) : base(socket)
    {
        _writer = writer;
        _source = new IPEndPoint(source.Address.MapToIPv6(), source.Port);
        _dest = new IPEndPoint(dest.Address.MapToIPv6(), dest.Port);
    }

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer, CancellationToken ct = default)
    {
        var count = await base.ReadAsync(buffer, ct);

        if (_writer is not null && count > 0)
            _writer.Write(new TrafficPacket(
                _dest, _source, buffer[..count].ToArray(), DateTimeOffset.UtcNow));

        return count;
    }

    public override ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
    {
        if (_writer is not null && buffer.Length > 0)
            _writer.Write(new TrafficPacket(
                _source, _dest, buffer.ToArray(), DateTimeOffset.UtcNow));

        return base.WriteAsync(buffer, ct);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_writer is not null)
            await _writer.DisposeAsync();

        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
