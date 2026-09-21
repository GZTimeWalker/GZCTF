using GZCTF.Features.Dashboard.Domain;
using GZCTF.Middlewares;
using GZCTF.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.Dashboard.Api;

[RequireAdmin]
[ApiController]
[Route("api/admin/cohorts")]
public sealed class AdminCohortsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CohortResponse>>> List(CancellationToken token) =>
        Ok(await db.Cohorts.AsNoTracking().OrderBy(item => item.Name)
            .Select(item => new CohortResponse(item.Id, item.Name, item.IsActive, item.Users.Count))
            .ToArrayAsync(token));

    [HttpPost]
    public async Task<ActionResult<CohortResponse>> Create([FromBody] CohortCommand command, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return BadRequest();
        var cohort = new Cohort { Name = command.Name.Trim(), IsActive = true };
        db.Cohorts.Add(cohort);
        try
        {
            await db.SaveChangesAsync(token);
        }
        catch (DbUpdateException)
        {
            return Conflict();
        }
        return Ok(new CohortResponse(cohort.Id, cohort.Name, cohort.IsActive, 0));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Rename(Guid id, [FromBody] CohortCommand command, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return BadRequest();
        var cohort = await db.Cohorts.SingleOrDefaultAsync(item => item.Id == id, token);
        if (cohort is null) return NotFound();
        cohort.Name = command.Name.Trim();
        try { await db.SaveChangesAsync(token); } catch (DbUpdateException) { return Conflict(); }
        return NoContent();
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] CohortStatusCommand command, CancellationToken token)
    {
        var cohort = await db.Cohorts.SingleOrDefaultAsync(item => item.Id == id, token);
        if (cohort is null) return NotFound();
        cohort.IsActive = command.IsActive;
        await db.SaveChangesAsync(token);
        return NoContent();
    }

    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<CohortMemberResponse>>> Members(
        Guid id, [FromQuery] string? search, CancellationToken token)
    {
        if (!await db.Cohorts.AnyAsync(item => item.Id == id, token)) return NotFound();
        var query = db.Users.AsNoTracking().Where(item => item.CohortId == id);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(item => item.UserName != null && EF.Functions.ILike(item.UserName, $"%{search}%"));
        return Ok(await query.OrderBy(item => item.UserName)
            .Select(item => new CohortMemberResponse(item.Id, item.UserName ?? string.Empty))
            .ToArrayAsync(token));
    }

    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] CohortMembersCommand command, CancellationToken token)
    {
        var cohort = await db.Cohorts.SingleOrDefaultAsync(item => item.Id == id, token);
        if (cohort is null) return NotFound();
        var ids = command.UserIds.Distinct().ToArray();
        var users = await db.Users.Where(item => ids.Contains(item.Id)).ToListAsync(token);
        if (users.Count != ids.Length) return BadRequest();
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        foreach (var user in users) user.CohortId = id;
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return NoContent();
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> Clear(Guid id, Guid userId, CancellationToken token)
    {
        var updated = await db.Users.Where(item => item.Id == userId && item.CohortId == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.CohortId, (Guid?)null), token);
        return updated == 0 ? NotFound() : NoContent();
    }
}

public sealed record CohortCommand(string Name);
public sealed record CohortStatusCommand(bool IsActive);
public sealed record CohortMembersCommand(IReadOnlyList<Guid> UserIds);
public sealed record CohortResponse(Guid Id, string Name, bool IsActive, int MemberCount);
public sealed record CohortMemberResponse(Guid Id, string UserName);
