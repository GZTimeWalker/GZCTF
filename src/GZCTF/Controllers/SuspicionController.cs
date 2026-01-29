using GZCTF.Models;
using GZCTF.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Controllers;

[Route("api/admin/suspicion")]
[Authorize(Roles = "Admin")]
[ApiController]
public class SuspicionController(AppDbContext dbContext, ILogger<SuspicionController> logger) : ControllerBase
{
    [HttpGet("rules")]
    public async Task<ActionResult<List<SuspicionRule>>> GetRules(CancellationToken token)
    {
        return await dbContext.SuspicionRules.ToListAsync(token);
    }

    [HttpPut("rules/{code}")]
    public async Task<IActionResult> UpdateRule(string code, [FromBody] SuspicionRule model, CancellationToken token)
    {
        var rule = await dbContext.SuspicionRules.SingleOrDefaultAsync(r => r.RuleCode == code, token);
        if (rule == null)
        {
            rule = new SuspicionRule { RuleCode = code };
            dbContext.SuspicionRules.Add(rule);
        }

        rule.Weight = model.Weight;
        rule.Description = model.Description;
        
        await dbContext.SaveChangesAsync(token);
        return Ok(rule);
    }

    [HttpPost("participations/{id:int}/restore")]
    public async Task<IActionResult> RestoreParticipation(int id, CancellationToken token)
    {
        var participation = await dbContext.Participations
            .Include(p => p.Team)
            .SingleOrDefaultAsync(p => p.Id == id, token);

        if (participation == null) return NotFound();

        participation.Status = ParticipationStatus.Accepted;
        // Optionally reset score? Or just allow them to continue.
        // If score is high, next event will freeze them again.
        // So we might want to reduce score?
        // Let's assume Admin will also dismiss specific events if they are false positives.
        
        await dbContext.SaveChangesAsync(token);
        logger.LogInformation("Restored participation {Id} for team {Team}", id, participation.Team.Name);
        return Ok();
    }

    [HttpDelete("events/{eventId:int}")]
    public async Task<IActionResult> DismissEvent(int eventId, CancellationToken token)
    {
        var evt = await dbContext.SuspicionEvents
            .Include(e => e.Participation)
            .SingleOrDefaultAsync(e => e.Id == eventId, token);

        if (evt == null) return NotFound();

        var part = evt.Participation;
        part.SuspicionScore -= evt.ScoreDelta;
        if (part.SuspicionScore < 0) part.SuspicionScore = 0;
        
        dbContext.SuspicionEvents.Remove(evt);
        await dbContext.SaveChangesAsync(token);
        
        logger.LogInformation("Dismissed suspicion event {EventId} for participation {PartId}", eventId, part.Id);
        return Ok();
    }
}
