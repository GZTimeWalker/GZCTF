using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using FluentStorage.Blobs;
using PacketDotNet;
using PacketDotNet.Utils;
using SharpPcap;
using SharpPcap.LibPcap;

namespace GZCTF.Utils;

public class RecordableNetworkStreamOptions
{
    /// <summary>
    /// The source address of the traffic
    /// </summary>
    public IPEndPoint Source { get; init; } = new(0, 0);

    /// <summary>
    /// The destination address of the traffic
    /// </summary>
    public IPEndPoint Dest { get; init; } = new(0, 0);

    /// <summary>
    /// The path to store the captured traffic
    /// </summary>
    public string BlobPath { get; init; } = string.Empty;

    /// <summary>
    /// Is the capture enabled
    /// </summary>
    public bool EnableCapture { get; init; }
}

/// <summary>
/// The network stream that can record the traffic
/// </summary>
public sealed class RecordableNetworkStream : NetworkStream
{
    static readonly PhysicalAddress DummyPhysicalAddress = PhysicalAddress.Parse("00-11-00-11-00-11");
    static readonly IPEndPoint Host = new(0, 65535);

    readonly CaptureFileWriterDevice? _device;
    readonly RecordableNetworkStreamOptions _options;
    readonly IBlobStorage? _storage;
    readonly string _tempFile = string.Empty;

    bool _disposed;

    public RecordableNetworkStream(Socket socket, byte[]? metadata, IBlobStorage storage,
        RecordableNetworkStreamOptions options) :
        base(socket)
    {
        _options = options;

        options.Source.Address = options.Source.Address.MapToIPv6();
        options.Dest.Address = options.Dest.Address.MapToIPv6();

        if (!_options.EnableCapture || string.IsNullOrEmpty(_options.BlobPath))
            return;

        _storage = storage;
        _tempFile = Path.GetTempFileName();

        _device = new(_tempFile, FileMode.Open);

        _device.Open();

        if (metadata is not null)
            WriteCapturedData(Host, _options.Source, metadata);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var count = await base.ReadAsync(buffer, cancellationToken);

        if (!_options.EnableCapture)
            return count;

        WriteCapturedData(_options.Dest, _options.Source, buffer[..count]);

        return count;
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_options.EnableCapture)
            WriteCapturedData(_options.Source, _options.Dest, buffer);

        return base.WriteAsync(buffer, cancellationToken);
    }

    /// <summary>
    /// Write the captured data to the file
    /// </summary>
    /// <param name="source">Source address</param>
    /// <param name="dest">Destination address</param>
    /// <param name="buffer">Data buffer</param>
    void WriteCapturedData(IPEndPoint source, IPEndPoint dest, ReadOnlyMemory<byte> buffer)
    {
        var udp = new UdpPacket((ushort)source.Port, (ushort)dest.Port)
        {
            PayloadDataSegment = new ByteArraySegment(buffer.ToArray())
        };

        var packet = new EthernetPacket(DummyPhysicalAddress, DummyPhysicalAddress, EthernetType.IPv6)
        {
            PayloadPacket = new IPv6Packet(source.Address, dest.Address) { PayloadPacket = udp }
        };

        udp.UpdateUdpChecksum();

        _device?.Write(new RawCapture(LinkLayers.Ethernet, new(), packet.Bytes));
    }

    public override async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _device?.Close();
        _device?.Dispose();

        // move temp file to storage with specified path
        if (_options.EnableCapture && !string.IsNullOrEmpty(_options.BlobPath) && _storage is not null)
        {
            await _storage.WriteFileAsync(_options.BlobPath, _tempFile);
            File.Delete(_tempFile);
        }

        await base.DisposeAsync();
        GC.SuppressFinalize(this);

        _disposed = true;
    }
}
