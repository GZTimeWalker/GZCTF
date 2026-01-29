using GZCTF.Models;
using GZCTF.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace GZCTF.Services;

public class SuspicionService(
    IServiceScopeFactory scopeFactory,
    ILogger<SuspicionService> logger,
    IStringLocalizer<Program> localizer) : ISuspicionService
{
    public async Task AddSuspicion(Participation participation, string ruleCode, string details, CancellationToken token = default)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var rule = await dbContext.SuspicionRules
            .SingleOrDefaultAsync(r => r.RuleCode == ruleCode, token);

        int weight = rule?.Weight ?? GetDefaultWeight(ruleCode);
        
        // If rule not found, maybe log it or use default? 
        // For now, let's assume we might seed rules later or they are dynamic.
        if (rule == null)
        {
             logger.LogInformation("Suspicion rule {RuleCode} not found, using default weight {Weight}.", ruleCode, weight);
        }

        var participationEntry = await dbContext.Participations
            .Include(p => p.SuspicionEvents)
            .Include(p => p.Team)
            .SingleOrDefaultAsync(p => p.Id == participation.Id, token);

        if (participationEntry == null) return;

        // Check for duplicate event (Idempotency)
        // We consider an event duplicate if it has the same Type and Details
        // within a short time window (e.g., 5 minutes for dynamic events) or just strictly same details.
        // For static analysis (CheatReportController recalculations), strict equality on Details is best.
        var existingEvent = participationEntry.SuspicionEvents
            .FirstOrDefault(e => e.Type == ruleCode && e.Details == details);

        if (existingEvent != null) return;
        
        var evt = new SuspicionEvent
        {
            ParticipationId = participationEntry.Id,
            Type = ruleCode,
            ScoreDelta = weight,
            Details = details,
            TimeUtc = DateTimeOffset.UtcNow
        };
        
        dbContext.SuspicionEvents.Add(evt);
        participationEntry.SuspicionEvents.Add(evt);
        participationEntry.SuspicionScore += weight;





        await dbContext.SaveChangesAsync(token);
    }

    public async Task<int> GetScore(Participation participation, CancellationToken token = default)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var p = await dbContext.Participations.FindAsync([participation.Id], token);
        return p?.SuspicionScore ?? 0;
    }

    private static int GetDefaultWeight(string ruleCode)
    {
        return ruleCode switch
        {
            "StolenFlag" => 100,
            "SharedIP" => 10,
            "UnknownIP" => 10,
            "CrossTeamIP" => 40,
            "TokenAbuse" => 60,
            "Hoarding" => 30,
            "Burst" => 40,
            "NoDownload" => 60,
            "NoContainer" => 60,
            "FastSolve-Open" => 50,
            "FastSolve-Download" => 50,
            "FastSolve-Container" => 50,
            "SequenceSimilarity" => 40,
            _ => 10
        };
    }
}
