using System.Text.Json.Serialization;

namespace GZCTF.Models.Internal;

/// <summary>
/// Direction of a captured payload chunk relative to the proxied container.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<TrafficFlowDirection>))]
public enum TrafficFlowDirection
{
    /// Container emitted bytes that were forwarded out to the team's WebSocket.
    ContainerToTeam = 0,

    /// Team-side WebSocket sent bytes that were forwarded into the container.
    TeamToContainer = 1
}

/// <summary>
/// Compact summary of a single proxied TCP session, used in the inspector
/// flow-list table. Multiple summaries are returned for one pcap file.
/// </summary>
public class TrafficFlowSummary
{
    /// <summary>
    /// Per-connection identifier in the synthetic UDP-over-IPv6 capture format
    /// (range 10001..65000, allocated by <see cref="GZCTF.Services.Traffic.TrafficRecorder.TryAcquire"/>).
    /// </summary>
    public int ConnectionPort { get; set; }

    public DateTimeOffset FirstSeenUtc { get; set; }

    public DateTimeOffset LastSeenUtc { get; set; }

    /// <summary>
    /// Peer IP carried by the IPv6 source/destination header — the team-side
    /// participant address as seen at the proxy boundary.
    /// </summary>
    public string PeerIp { get; set; } = string.Empty;

    public int PacketsIn { get; set; }

    public int PacketsOut { get; set; }

    public long BytesIn { get; set; }

    public long BytesOut { get; set; }

    /// <summary>
    /// Number of flag occurrences detected anywhere in this flow's payload.
    /// </summary>
    public int FlagHits { get; set; }
}

/// <summary>
/// One contiguous payload chunk in a flow, in the order it was emitted.
/// </summary>
public sealed class TrafficFlowChunk
{
    public TrafficFlowDirection Direction { get; set; }

    public DateTimeOffset TimestampUtc { get; set; }

    /// <summary>
    /// Raw payload bytes for this chunk, base64-encoded for JSON transport.
    /// </summary>
    public string PayloadBase64 { get; set; } = string.Empty;

    /// <summary>
    /// Byte offsets within <see cref="PayloadBase64"/> (decoded) where a
    /// known flag begins. Empty when no flag matches.
    /// </summary>
    public int[] FlagOffsets { get; set; } = [];
}

/// <summary>
/// Full payload detail of a single flow, including every captured chunk.
/// </summary>
public sealed class TrafficFlowDetail : TrafficFlowSummary
{
    public TrafficFlowChunk[] Chunks { get; set; } = [];
}

/// <summary>
/// Query-binding DTO for the flow-list endpoint. All fields optional.
/// </summary>
public sealed class FlowFilter
{
    /// <summary>
    /// .NET regex applied to the ASCII rendering of each flow's combined
    /// payload (both directions concatenated). When set, only matching flows
    /// are returned.
    /// </summary>
    public string? RegexPattern { get; set; }

    /// <summary>
    /// Substring match against <see cref="TrafficFlowSummary.PeerIp"/>.
    /// </summary>
    public string? PeerIpContains { get; set; }

    public DateTimeOffset? StartUtc { get; set; }

    public DateTimeOffset? EndUtc { get; set; }

    public TrafficFlowDirection? Direction { get; set; }

    /// <summary>
    /// When true, only flows whose <see cref="TrafficFlowSummary.FlagHits"/>
    /// is &gt; 0 are returned.
    /// </summary>
    public bool FlagsOnly { get; set; }
}
