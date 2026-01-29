using GZCTF.Models;
using GZCTF.Models.Data;
using Microsoft.EntityFrameworkCore;
using GZCTF.Models.Internal;
using Microsoft.Extensions.Localization;

namespace GZCTF.Services;

public class SuspicionService(
    IServiceScopeFactory scopeFactory,
    ILogger<SuspicionService> logger,
    IStringLocalizer<Program> localizer) : ISuspicionService
{
    public async Task AddSuspicion(Participation participation, string ruleCode, string details, int? relatedParticipationId = null, CancellationToken token = default)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var rule = await dbContext.SuspicionRules
            .SingleOrDefaultAsync(r => r.RuleCode == ruleCode, token);

        int weight = rule?.Weight ?? GetDefaultWeight(ruleCode);
        
        if (rule == null)
        {
             logger.LogInformation("Suspicion rule {RuleCode} not found, using default weight {Weight}.", ruleCode, weight);
        }

        // Optimization: Use AnyAsync directly on the DbSet instead of loading the entity with includes
        var exists = await dbContext.SuspicionEvents
            .AnyAsync(e => e.ParticipationId == participation.Id && 
                           e.Type == ruleCode && 
                           e.Details == details, token);

        if (exists) return;

        // We need to fetch the participation to update the score, but we don't need to load all events
        var participationEntry = await dbContext.Participations
            .SingleOrDefaultAsync(p => p.Id == participation.Id, token);

        if (participationEntry == null) return;
        
        var evt = new SuspicionEvent
        {
            ParticipationId = participationEntry.Id,
            Type = ruleCode,
            ScoreDelta = weight,
            Details = details,
            TimeUtc = DateTimeOffset.UtcNow,
            GameId = participation.GameId,
            RelatedParticipationId = relatedParticipationId
        };
        
        dbContext.SuspicionEvents.Add(evt);
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
            SuspicionType.StolenFlag => 100,
            SuspicionType.SharedIP => 10,
            SuspicionType.UnknownIP => 10,
            SuspicionType.CrossTeamIP => 40,
            SuspicionType.TokenAbuse => 60,
            SuspicionType.Hoarding => 30,
            SuspicionType.Burst => 40,
            SuspicionType.NoDownload => 60,
            SuspicionType.NoContainer => 60,
            SuspicionType.FastSolveOpen => 50,
            SuspicionType.FastSolveDownload => 50,
            SuspicionType.FastSolveContainer => 50,
            SuspicionType.SequenceSimilarity => 40,
            _ => 10
        };
    }
}
