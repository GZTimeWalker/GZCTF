using System.Net;

namespace GZCTF.Services.Traffic;

/// <summary>
/// A single captured traffic segment to be written into a pcap file.
/// </summary>
public readonly struct TrafficPacket(
    IPEndPoint source,
    IPEndPoint dest,
    byte[] data,
    DateTimeOffset timestamp)
{
    public IPEndPoint Source { get; } = source;
    public IPEndPoint Dest { get; } = dest;
    public byte[] Data { get; } = data;
    public DateTimeOffset Timestamp { get; } = timestamp;
}
