using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using PacketDotNet;
using PacketDotNet.Utils;
using SharpPcap;
using SharpPcap.LibPcap;

namespace GZCTF.Utils;

public class CapturableNetworkStreamOptions
{
    /// <summary>
    /// 流量源地址
    /// </summary>
    public required IPEndPoint Source { get; set; }

    /// <summary>
    /// 流量目的地址
    /// </summary>
    public required IPEndPoint Dest { get; set; }

    /// <summary>
    /// 记录文件位置
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 启用文件流量捕获
    /// </summary>
    public bool EnableCapture { get; set; } = false;
}

/// <summary>
/// 能够被捕获的网络流（Socket）
/// </summary>
public sealed class CapturableNetworkStream : NetworkStream
{
    private readonly CapturableNetworkStreamOptions _options;
    private readonly CaptureFileWriterDevice? _device = null;
    private readonly PhysicalAddress _dummyPhysicalAddress = PhysicalAddress.Parse("00-11-00-11-00-11");

    public CapturableNetworkStream(Socket socket, CapturableNetworkStreamOptions options) : base(socket)
    {
        _options = options;

        options.Source.Address = options.Source.Address.MapToIPv6();
        options.Dest.Address = options.Dest.Address.MapToIPv6();

        if (_options.EnableCapture && !string.IsNullOrEmpty(_options.FilePath))
        {
            var dir = Path.GetDirectoryName(_options.FilePath);
            if (!Path.Exists(dir) && dir is not null)
                Directory.CreateDirectory(dir);

            _device = new(_options.FilePath, FileMode.Open);
            _device.Open(LinkLayers.Ethernet);
        }
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var count = await base.ReadAsync(buffer, cancellationToken);

        if (!_options.EnableCapture)
            return count;

        var udp = new UdpPacket((ushort)_options.Dest.Port, (ushort)_options.Source.Port)
        {
            PayloadDataSegment = new ByteArraySegment(buffer[..count].ToArray())
        };

        var packet = new EthernetPacket(_dummyPhysicalAddress, _dummyPhysicalAddress, EthernetType.IPv6)
        {
            PayloadPacket = new IPv6Packet(_options.Dest.Address, _options.Source.Address) { PayloadPacket = udp }
        };

        udp.UpdateUdpChecksum();

        _device?.Write(new RawCapture(LinkLayers.Ethernet, new(), packet.Bytes));

        return count;
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_options.EnableCapture)
        {
            var udp = new UdpPacket((ushort)_options.Source.Port, (ushort)_options.Dest.Port)
            {
                PayloadDataSegment = new ByteArraySegment(buffer.ToArray())
            };

            var packet = new EthernetPacket(_dummyPhysicalAddress, _dummyPhysicalAddress, EthernetType.IPv6)
            {
                PayloadPacket = new IPv6Packet(_options.Source.Address, _options.Dest.Address) { PayloadPacket = udp }
            };

            udp.UpdateUdpChecksum();

            _device?.Write(new RawCapture(LinkLayers.Ethernet, new(), packet.Bytes));
        }

        await base.WriteAsync(buffer, cancellationToken);
    }

    public override void Close()
    {
        base.Close();
        _device?.Close();
    }
}
