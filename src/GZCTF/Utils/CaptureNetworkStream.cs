using System.Net;
using System.Net.Sockets;
using GZCTF.Models.Data;
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
        {
            var span = buffer.Span[..count];
            var ts = DateTimeOffset.UtcNow;
            // Container → team direction: flag exfil shows up here.
            writer.Inspect(span, FlagEgressDirection.ContainerToTeam, _dest, _source, ts);
            writer.Write(new(_dest, _source, span.ToArray(), ts));
        }

        return count;
    }

    public override ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
    {
        if (writer is not null && buffer.Length > 0)
        {
            var span = buffer.Span;
            var ts = DateTimeOffset.UtcNow;
            // Team → container direction: replays / unusual submissions show up here.
            writer.Inspect(span, FlagEgressDirection.TeamToContainer, _source, _dest, ts);
            writer.Write(new(_source, _dest, buffer.ToArray(), ts));
        }

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
