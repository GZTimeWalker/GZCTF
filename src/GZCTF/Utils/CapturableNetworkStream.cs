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
    public IPEndPoint Source { get; set; } = new(0, 0);

    /// <summary>
    /// 流量目的地址
    /// </summary>
    public IPEndPoint Dest { get; set; } = new(0, 0);

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
    private readonly IPEndPoint _host = new(0, 65535);

    public CapturableNetworkStream(Socket socket, byte[]? metadata, CapturableNetworkStreamOptions options) : base(socket)
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

            if (metadata is not null)
                WriteCapturedData(_host, _options.Source, metadata);
        }
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var count = await base.ReadAsync(buffer, cancellationToken);

        if (!_options.EnableCapture)
            return count;

        WriteCapturedData(_options.Dest, _options.Source, buffer[..count]);

        return count;
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_options.EnableCapture)
            WriteCapturedData(_options.Source, _options.Dest, buffer);

        await base.WriteAsync(buffer, cancellationToken);
    }

    /// <summary>
    /// 向文件写入一条流量记录
    /// </summary>
    /// <param name="source">源地址</param>
    /// <param name="dest">目的地址</param>
    /// <param name="buffer">数据</param>
    internal void WriteCapturedData(IPEndPoint source, IPEndPoint dest, ReadOnlyMemory<byte> buffer)
    {
        var udp = new UdpPacket((ushort)source.Port, (ushort)dest.Port)
        {
            PayloadDataSegment = new ByteArraySegment(buffer.ToArray())
        };

        var packet = new EthernetPacket(_dummyPhysicalAddress, _dummyPhysicalAddress, EthernetType.IPv6)
        {
            PayloadPacket = new IPv6Packet(source.Address, dest.Address) { PayloadPacket = udp }
        };

        udp.UpdateUdpChecksum();

        _device?.Write(new RawCapture(LinkLayers.Ethernet, new(), packet.Bytes));
    }

    public override void Close()
    {
        base.Close();
        _device?.Close();
    }
}
