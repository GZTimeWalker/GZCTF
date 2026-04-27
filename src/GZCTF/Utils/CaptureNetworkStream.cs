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
public sealed class CaptureNetworkStream(
    Socket socket,
    TrafficWriter? writer,
    IPEndPoint source,
    IPEndPoint dest)
    : NetworkStream(socket)
{
    readonly IPEndPoint _source = new(source.Address.MapToIPv6(), source.Port);
    readonly IPEndPoint _dest = new(dest.Address.MapToIPv6(), dest.Port);
    int _disposed;

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer, CancellationToken ct = default)
    {
        var count = await base.ReadAsync(buffer, ct);

        if (writer is not null && count > 0)
            writer.Write(new(
                _dest, _source, buffer[..count].ToArray(), DateTimeOffset.UtcNow));

        return count;
    }

    public override ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
    {
        if (writer is not null && buffer.Length > 0)
            writer.Write(new(
                _source, _dest, buffer.ToArray(), DateTimeOffset.UtcNow));

        return base.WriteAsync(buffer, ct);
    }

    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        writer?.Dispose();

        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
