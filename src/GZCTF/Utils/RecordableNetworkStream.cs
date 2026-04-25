using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using GZCTF.Services.Capture;

namespace GZCTF.Utils;

public class RecordableNetworkStreamOptions
{
    /// <summary>
    /// The source address of the traffic (client side)
    /// </summary>
    public IPEndPoint Source { get; init; } = new(0, 0);

    /// <summary>
    /// The destination address of the traffic (container side)
    /// </summary>
    public IPEndPoint Dest { get; init; } = new(0, 0);

    /// <summary>
    /// Is the capture enabled
    /// </summary>
    public bool EnableCapture { get; init; }
}

/// <summary>
/// A network stream that records traffic by sending captured packets to a shared TrafficRecorder.
/// Instead of writing pcap files directly, packets are forwarded through the recorder's channel
/// so that concurrent connections to the same container share a single pcap output.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Network stream recording requires platform proxy support")]
public sealed class RecordableNetworkStream : NetworkStream
{
    readonly RecordableNetworkStreamOptions _options;
    readonly TrafficRecorder? _recorder;

    bool _disposed;

    public RecordableNetworkStream(Socket socket, RecordableNetworkStreamOptions options,
        TrafficRecorder? recorder = null) :
        base(socket)
    {
        _options = options;
        _recorder = options.EnableCapture ? recorder : null;

        if (_recorder is not null)
        {
            options.Source.Address = options.Source.Address.MapToIPv6();
            options.Dest.Address = options.Dest.Address.MapToIPv6();
        }
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var count = await base.ReadAsync(buffer, cancellationToken);

        if (_recorder is not null && count > 0)
            WriteCapturedData(_options.Dest, _options.Source, buffer[..count]);

        return count;
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_recorder is not null && buffer.Length > 0)
            WriteCapturedData(_options.Source, _options.Dest, buffer);

        return base.WriteAsync(buffer, cancellationToken);
    }

    /// <summary>
    /// Send captured data to the shared TrafficRecorder via its channel.
    /// The data is copied because the original buffer may be returned to ArrayPool.
    /// </summary>
    /// <param name="source">Source address</param>
    /// <param name="dest">Destination address</param>
    /// <param name="buffer">Data buffer</param>
    void WriteCapturedData(IPEndPoint source, IPEndPoint dest, ReadOnlyMemory<byte> buffer) =>
        _recorder?.WritePacket(new CapturePacket(source, dest, buffer.ToArray(), DateTimeOffset.UtcNow));

    public override async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        // Note: we do NOT dispose/flush the recorder here.
        // The recorder is shared across connections and managed by TrafficRecorderRegistry.
        // Connection unregistration is handled by ProxyController.

        await base.DisposeAsync();
        GC.SuppressFinalize(this);

        _disposed = true;
    }
}
