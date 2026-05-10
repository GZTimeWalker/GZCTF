namespace GZCTF.Models.Internal;

/// <summary>
/// Platform-wide protocol/port honeypot configuration.
/// Default-disabled. Bind only ports the operator knows are free
/// (don't co-bind with the host's real sshd, postgres, etc.).
/// </summary>
public class HoneypotConfig
{
    /// <summary>
    /// Master switch. When false, no port listeners are started.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// IP address the listeners bind to. "0.0.0.0" for all interfaces,
    /// or a specific player-facing IP if the host is multi-homed.
    /// </summary>
    public string ListenAddress { get; set; } = "0.0.0.0";

    /// <summary>
    /// Per-service listener configuration.
    /// </summary>
    public List<HoneypotPort> Ports { get; set; } = [];

    /// <summary>
    /// Chain detector — escalates participations that hit multiple distinct baits.
    /// </summary>
    public bool ChainEnabled { get; set; } = true;

    /// <summary>
    /// Distinct bait count within ChainWindowMinutes that triggers a HoneypotChain signal.
    /// </summary>
    public int ChainThreshold { get; set; } = 3;

    /// <summary>
    /// Sliding window the chain detector considers when grouping hits per participation.
    /// </summary>
    public int ChainWindowMinutes { get; set; } = 30;

    /// <summary>
    /// How often the chain detector sweeps the SuspicionEvent table.
    /// </summary>
    public int ChainSweepIntervalSeconds { get; set; } = 60;
}

public class HoneypotPort
{
    /// <summary>
    /// Service tag used in the bait identifier (e.g., "ssh", "redis", "mysql").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// TCP port to bind.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Optional banner to send on connect. Null/empty for protocols that
    /// expect the client to speak first (Redis, Postgres, MongoDB, Memcached).
    /// </summary>
    public string? Banner { get; set; }

    /// <summary>
    /// Per-port toggle so individual services can be disabled without removing them.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
