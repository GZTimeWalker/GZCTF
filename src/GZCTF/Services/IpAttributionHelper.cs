using System.Net;
using GZCTF.Models;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Services;

public interface IIpAttributionHelper
{
    /// <summary>
    /// Resolve the most recent IP this user was seen using within
    /// <paramref name="window"/> of <paramref name="center"/>. Reads
    /// <see cref="GZCTF.Models.Data.LogModel"/>. Returns null if no log entry
    /// matches the (UserName, TimeUtc) window.
    /// </summary>
    Task<IPAddress?> ResolveUserIpAt(
        AppDbContext db,
        string userName,
        DateTimeOffset center,
        TimeSpan window,
        CancellationToken token);
}

/// <summary>
/// Joins the <c>Logs</c> table on UserName + TimeUtc to recover the most
/// recent IP a user was seen using around a given moment. Used by
/// access-time and submission-time cheat detectors to compare an
/// observed IP against a user's recent activity IPs.
///
/// Implementation note: the lookup pattern mirrors the private one inside
/// <c>HoneypotService.ResolveAttribution</c> but is intentionally a
/// separate class — that path is left untouched to avoid behavioral
/// regression on the honeypot pipeline.
/// </summary>
public sealed class IpAttributionHelper : IIpAttributionHelper
{
    public async Task<IPAddress?> ResolveUserIpAt(
        AppDbContext db,
        string userName,
        DateTimeOffset center,
        TimeSpan window,
        CancellationToken token)
    {
        if (string.IsNullOrEmpty(userName))
            return null;

        var min = center - window;
        var max = center + window;

        var match = await db.Logs
            .AsNoTracking()
            .Where(l => l.UserName == userName
                        && l.RemoteIP != null
                        && l.TimeUtc >= min
                        && l.TimeUtc <= max)
            .OrderByDescending(l => l.TimeUtc)
            .Select(l => l.RemoteIP)
            .FirstOrDefaultAsync(token);

        return match;
    }
}
