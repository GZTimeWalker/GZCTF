namespace GZCTF.Models.Internal;

/// <summary>
/// Configuration for the flag-egress tracer that scans proxied container
/// traffic for the team's dynamic flag bytes.
/// </summary>
public class FlagEgressConfig
{
    /// <summary>
    /// Master toggle. When false the inspector is never registered and
    /// the proxy hot path bypasses all scanning.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Sliding window for hit aggregation. Hits within this window from
    /// the same (participation, challenge, remote IP, direction) tuple
    /// are folded into a single event row.
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Packets smaller than this many bytes are skipped to avoid
    /// scan overhead on TCP control frames.
    /// </summary>
    public int MinPacketDataLength { get; set; } = 8;

    /// <summary>
    /// Cadence at which in-memory HitCount/LastSeenUtc updates are
    /// batched and persisted to the database.
    /// </summary>
    public int FlushIntervalSeconds { get; set; } = 5;
}
