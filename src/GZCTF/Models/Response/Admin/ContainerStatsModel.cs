namespace GZCTF.Models.Response.Admin;

/// <summary>
/// Point-in-time runtime stats for a single container instance, returned
/// by <c>GET /api/admin/instances/{guid}/stats</c>. Sampled on demand
/// from the underlying runtime (Docker.DotNet's
/// <c>GetContainerStatsAsync</c>). Kubernetes is not yet wired —
/// callers should treat a 404/null payload as "stats unavailable".
/// </summary>
public sealed class ContainerStatsModel
{
    /// <summary>CPU usage normalised to the host's online cores, 0–100 (× cores).</summary>
    public double CpuPercent { get; set; }

    public long MemoryUsedBytes { get; set; }
    public long MemoryLimitBytes { get; set; }

    /// <summary>Sum of <c>rx_bytes</c> across every interface.</summary>
    public long NetRxBytes { get; set; }
    /// <summary>Sum of <c>tx_bytes</c> across every interface.</summary>
    public long NetTxBytes { get; set; }

    public DateTimeOffset SampledAt { get; set; } = DateTimeOffset.UtcNow;
}
