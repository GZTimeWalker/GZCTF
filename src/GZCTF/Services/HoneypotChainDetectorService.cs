using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GZCTF.Services;

/// <summary>
/// Periodically aggregates honeypot hits per participation and escalates a
/// HoneypotChain HardSignal once the team has tripped enough distinct baits
/// inside the configured window. Walking through multiple cross-referenced
/// honeypots is the fingerprint of an automated link-following scanner or
/// AI agent — humans who hit one bait by mistake almost never hit several.
/// </summary>
public class HoneypotChainDetectorService(
    IServiceScopeFactory scopeFactory,
    IOptions<HoneypotConfig> config,
    ILogger<HoneypotChainDetectorService> logger) : BackgroundService
{
    private static readonly string[] HoneypotEventTypes =
    [
        SuspicionType.HoneypotHit,
        SuspicionType.HoneypotProtocolHit
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cfg = config.Value;
        if (!cfg.ChainEnabled)
        {
            logger.LogInformation("Honeypot chain detector disabled.");
            return;
        }

        var sweep = TimeSpan.FromSeconds(Math.Max(10, cfg.ChainSweepIntervalSeconds));
        var window = TimeSpan.FromMinutes(Math.Max(1, cfg.ChainWindowMinutes));
        var threshold = Math.Max(2, cfg.ChainThreshold);

        logger.LogInformation(
            "Honeypot chain detector running: window={Window} threshold={Threshold} sweep={Sweep}",
            window, threshold, sweep);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepOnce(window, threshold, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Honeypot chain detector sweep failed");
            }

            try
            {
                await Task.Delay(sweep, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task SweepOnce(TimeSpan window, int threshold, CancellationToken token)
    {
        var since = DateTimeOffset.UtcNow - window;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var suspicion = scope.ServiceProvider.GetRequiredService<ISuspicionService>();

        var events = await db.SuspicionEvents
            .AsNoTracking()
            .Where(e => e.TimeUtc >= since && HoneypotEventTypes.Contains(e.Type))
            .Select(e => new { e.ParticipationId, e.Details })
            .ToListAsync(token);

        if (events.Count == 0) return;

        var groups = events
            .GroupBy(e => e.ParticipationId)
            .Select(g => new
            {
                ParticipationId = g.Key,
                Baits = g.Select(x => ExtractBait(x.Details))
                    .Where(b => !string.IsNullOrEmpty(b))
                    .Select(b => b!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(b => b, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            })
            .Where(g => g.Baits.Count >= threshold)
            .ToList();

        if (groups.Count == 0) return;

        var ids = groups.Select(g => g.ParticipationId).ToList();
        var participations = await db.Participations
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.GameId })
            .ToListAsync(token);
        var lookup = participations.ToDictionary(p => p.Id);

        foreach (var group in groups)
        {
            if (!lookup.TryGetValue(group.ParticipationId, out var part)) continue;

            var stub = new Participation { Id = part.Id, GameId = part.GameId };
            var details = $"baits={string.Join(',', group.Baits)} count={group.Baits.Count} window={(int)window.TotalMinutes}m";

            try
            {
                await suspicion.AddSuspicion(stub, SuspicionType.HoneypotChain, details, token: token);
                logger.LogWarning(
                    "HoneypotChain raised for participation={Pid} count={Count} baits={Baits}",
                    part.Id, group.Baits.Count, string.Join(',', group.Baits));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to add HoneypotChain for participation={Pid}", part.Id);
            }
        }
    }

    private static string? ExtractBait(string details)
    {
        if (string.IsNullOrEmpty(details)) return null;
        const string prefix = "bait=";
        var idx = details.IndexOf(prefix, StringComparison.Ordinal);
        if (idx < 0) return null;
        var start = idx + prefix.Length;
        var end = details.IndexOf(' ', start);
        return end < 0 ? details[start..] : details[start..end];
    }
}
