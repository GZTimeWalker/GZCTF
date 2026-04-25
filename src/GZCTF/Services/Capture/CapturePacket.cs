using System.Net;

namespace GZCTF.Services.Capture;

/// <summary>
/// Represents a single captured traffic packet to be written to a pcap file.
/// </summary>
/// <param name="Source">The source address of the packet</param>
/// <param name="Dest">The destination address of the packet</param>
/// <param name="Data">The raw payload data (must be a copy, not a rented buffer slice)</param>
/// <param name="Timestamp">The time the packet was captured</param>
public readonly record struct CapturePacket(
    IPEndPoint Source,
    IPEndPoint Dest,
    byte[] Data,
    DateTimeOffset Timestamp
);
