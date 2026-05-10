using System.Net;

namespace GZCTF.Services;

public interface IHoneypotService
{
    /// <summary>
    /// Record an HTTP-level honeypot hit: log it, attribute it to a participation if possible,
    /// emit a SuspicionEvent, and broadcast to the admin live feed.
    /// </summary>
    /// <param name="context">Incoming HTTP context.</param>
    /// <param name="bait">Bait identifier (path).</param>
    /// <param name="category">"http" for web routes.</param>
    /// <param name="ruleCode">Suspicion rule code (defaults to HoneypotHit).</param>
    /// <param name="token">Cancellation token.</param>
    Task RecordHit(
        HttpContext context,
        string bait,
        string category,
        string? ruleCode = null,
        CancellationToken token = default);

    /// <summary>
    /// Record a protocol/port honeypot hit (TCP listener). No HttpContext is available,
    /// so attribution falls back to recent IP→user matches in the application log.
    /// </summary>
    /// <param name="remoteIp">Remote endpoint IP.</param>
    /// <param name="bait">Bait identifier (e.g., "ssh:22", "redis:6379").</param>
    /// <param name="probe">First bytes received from the client, encoded for storage.</param>
    /// <param name="ruleCode">Suspicion rule code (defaults to HoneypotProtocolHit).</param>
    /// <param name="token">Cancellation token.</param>
    Task RecordTcpHit(
        IPAddress? remoteIp,
        string bait,
        string? probe,
        string? ruleCode = null,
        CancellationToken token = default);
}
