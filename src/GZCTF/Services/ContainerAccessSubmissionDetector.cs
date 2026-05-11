using System.Net;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GZCTF.Services;

public interface IContainerAccessSubmissionDetector
{
    /// <summary>
    /// Runs four access-event-based cheat checks against an accepted submission:
    /// <see cref="SuspicionType.DelayedSolveSubmission"/>,
    /// <see cref="SuspicionType.InstantSubmitAfterAccess"/>,
    /// <see cref="SuspicionType.SubmitterNeverAccessedContainer"/>, and
    /// <see cref="SuspicionType.AccessIpMismatchAtSubmission"/>.
    /// </summary>
    /// <param name="submission">An accepted submission. Must have <c>UserId</c>, <c>ParticipationId</c>, <c>ChallengeId</c>, <c>SubmitTimeUtc</c> populated and <c>User</c> navigation loaded for UserName resolution.</param>
    /// <param name="platformProxyEnabled">Pass <c>provider.PortMappingType == ContainerPortMappingType.PlatformProxy</c>. When false, the detector returns immediately (no proxy means no <see cref="ContainerAccessEvent"/> rows to correlate).</param>
    /// <param name="token">Cancellation token.</param>
    Task RunChecks(Submission submission, bool platformProxyEnabled, CancellationToken token = default);
}

public sealed class ContainerAccessSubmissionDetector(
    AppDbContext db,
    ISuspicionService suspicion,
    IIpAttributionHelper ipHelper,
    IOptions<CheatDetectionConfig> options,
    ILogger<ContainerAccessSubmissionDetector> logger) : IContainerAccessSubmissionDetector
{
    private static readonly TimeSpan SubmitterIpWindow = TimeSpan.FromSeconds(5);

    public async Task RunChecks(Submission submission, bool platformProxyEnabled, CancellationToken token = default)
    {
        if (!platformProxyEnabled)
            return;
        if (submission.UserId is null)
            return; // No user identity — nothing access-attributable to compare.

        var cfg = options.Value;
        var userId = submission.UserId.Value;

        // Single projection over all access events for this challenge.
        var rows = await db.ContainerAccessEvents
            .AsNoTracking()
            .Where(e => e.ChallengeId == submission.ChallengeId
                        && e.ConnectedAtUtc <= submission.SubmitTimeUtc)
            .Select(e => new
            {
                e.AccessingUserId,
                e.AccessingParticipationId,
                e.RemoteIp,
                e.ConnectedAtUtc,
            })
            .ToListAsync(token);

        if (rows.Count == 0)
            return; // No access events at all (e.g. challenge predates instrumentation, or no one accessed).

        var submitterRows = rows.Where(r => r.AccessingUserId == userId).ToList();
        var teamRows = rows.Where(r => r.AccessingParticipationId == submission.ParticipationId).ToList();

        var participation = new Participation
        {
            Id = submission.ParticipationId,
            GameId = submission.GameId,
        };

        // 1) DelayedSolveSubmission
        // 2) InstantSubmitAfterAccess
        if (submitterRows.Count > 0)
        {
            var firstAccess = submitterRows.Min(r => r.ConnectedAtUtc);
            var latency = submission.SubmitTimeUtc - firstAccess;
            // Clock-skew safety: negative latency (submission "before" first access by a few ms)
            // can happen across services; treat as zero.
            if (latency < TimeSpan.Zero) latency = TimeSpan.Zero;

            // Details use ';' field separators + ':' key/value separator and avoid
            // any other colons (no ISO timestamps in values) so the frontend
            // parseDetailLines in monitor/CheatInfo.tsx renders each field as
            // a clean label+value pair. Timestamps go to unix-seconds.
            if (latency.TotalMinutes > cfg.DelayedSubmissionThresholdMinutes)
            {
                try
                {
                    var details =
                        $"latencyMin:{(int)latency.TotalMinutes};" +
                        $"firstAccessUnix:{firstAccess.ToUnixTimeSeconds()};" +
                        $"submitUnix:{submission.SubmitTimeUtc.ToUnixTimeSeconds()}";
                    await suspicion.AddSuspicion(participation, SuspicionType.DelayedSolveSubmission, details, token: token);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "DelayedSolveSubmission raise failed for submission {Id}", submission.Id);
                }
            }
            else if (latency.TotalSeconds < cfg.InstantSubmitThresholdSeconds)
            {
                try
                {
                    var details =
                        $"latencyMs:{(int)latency.TotalMilliseconds};" +
                        $"firstAccessUnix:{firstAccess.ToUnixTimeSeconds()};" +
                        $"submitUnix:{submission.SubmitTimeUtc.ToUnixTimeSeconds()}";
                    await suspicion.AddSuspicion(participation, SuspicionType.InstantSubmitAfterAccess, details, token: token);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "InstantSubmitAfterAccess raise failed for submission {Id}", submission.Id);
                }
            }
        }

        // 3) SubmitterNeverAccessedContainer — submitter user didn't, but a teammate did.
        if (submitterRows.Count == 0 && teamRows.Count > 0)
        {
            try
            {
                var teamAccessUserIds = string.Join(',',
                    teamRows.Select(r => r.AccessingUserId)
                            .Where(g => g.HasValue)
                            .Distinct()
                            .Take(8)
                            .Select(g => g!.Value.ToString()));

                var details = $"submitterUserId:{userId};teamAccessUserIds:{teamAccessUserIds}";
                await suspicion.AddSuspicion(participation, SuspicionType.SubmitterNeverAccessedContainer, details, token: token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SubmitterNeverAccessedContainer raise failed for submission {Id}", submission.Id);
            }
        }

        // 4) AccessIpMismatchAtSubmission — only when we have submitter access events to compare against.
        if (submitterRows.Count > 0)
        {
            try
            {
                var submitterAccessIps = submitterRows
                    .Select(r => r.RemoteIp)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct(StringComparer.Ordinal)
                    .ToHashSet(StringComparer.Ordinal);

                var userName = submission.User?.UserName;
                if (!string.IsNullOrEmpty(userName) && submitterAccessIps.Count > 0)
                {
                    var submitterIp = await ipHelper.ResolveUserIpAt(db, userName, submission.SubmitTimeUtc, SubmitterIpWindow, token);
                    if (submitterIp is not null)
                    {
                        var submitterIpString = NormalizeIp(submitterIp);
                        if (!submitterAccessIps.Contains(submitterIpString))
                        {
                            var details =
                                $"submitterIp:{submitterIpString};" +
                                $"accessIps:{string.Join(',', submitterAccessIps.Take(8))};" +
                                $"userId:{userId}";
                            await suspicion.AddSuspicion(participation, SuspicionType.AccessIpMismatchAtSubmission, details, token: token);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AccessIpMismatchAtSubmission check failed for submission {Id}", submission.Id);
            }
        }
    }

    private static string NormalizeIp(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        return ip.ToString();
    }
}
