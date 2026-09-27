using GZCTF.Features.Dashboard.Application;
using GZCTF.Features.Dashboard.Domain;
using GZCTF.Middlewares;
using GZCTF.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DashboardConfig = GZCTF.Features.Dashboard.Domain.Dashboard;

namespace GZCTF.Features.Dashboard.Api;

[RequireAdmin]
[ApiController]
[Route("api/admin/dashboards")]
public sealed class AdminDashboardsController(AppDbContext db, DashboardTokenService tokens) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminDashboardResponse>>> List(CancellationToken token) =>
        Ok(await db.Dashboards.AsNoTracking().OrderBy(item => item.Name).Select(item => new AdminDashboardResponse(
            item.Id, item.Name, item.TopCount, item.IsEnabled,
            item.Tokens.Count(current => current.RevokedAtUtc == null))).ToArrayAsync(token));

    [HttpPost]
    public async Task<ActionResult<AdminDashboardResponse>> Create([FromBody] DashboardCommand command, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || command.TopCount is not 10 and not 20) return BadRequest();
        var dashboard = new DashboardConfig { Name = command.Name.Trim(), TopCount = command.TopCount };
        db.Dashboards.Add(dashboard);
        await db.SaveChangesAsync(token);
        return Ok(new AdminDashboardResponse(dashboard.Id, dashboard.Name, dashboard.TopCount, dashboard.IsEnabled, 0));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] DashboardCommand command, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || command.TopCount is not 10 and not 20) return BadRequest();
        var dashboard = await db.Dashboards.SingleOrDefaultAsync(item => item.Id == id, token);
        if (dashboard is null) return NotFound();
        dashboard.Name = command.Name.Trim(); dashboard.TopCount = command.TopCount; dashboard.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(token);
        return NoContent();
    }

    [HttpPost("{id:guid}/tokens")]
    public async Task<ActionResult<DashboardTokenResult>> CreateToken(Guid id, [FromBody] TokenExpiryCommand command, CancellationToken token) =>
        (await tokens.CreateAsync(id, command.ExpiresAtUtc, token)) is { } result ? Ok(result) : NotFound();

    [HttpGet("{id:guid}/tokens")]
    public async Task<ActionResult<IReadOnlyList<DashboardTokenSummaryResponse>>> ListTokens(
        Guid id, CancellationToken token)
    {
        if (!await db.Dashboards.AsNoTracking().AnyAsync(item => item.Id == id, token))
            return NotFound();

        return Ok(await db.DashboardTokens.AsNoTracking()
            .Where(item => item.DashboardId == id && item.RevokedAtUtc == null)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new DashboardTokenSummaryResponse(
                item.Id, item.CreatedAtUtc, item.ExpiresAtUtc, item.LastUsedAtUtc))
            .ToArrayAsync(token));
    }

    [HttpPost("{id:guid}/tokens/{tokenId:guid}/rotate")]
    public async Task<ActionResult<DashboardTokenResult>> RotateToken(Guid id, Guid tokenId, [FromBody] TokenExpiryCommand command, CancellationToken token) =>
        (await tokens.RotateAsync(id, tokenId, command.ExpiresAtUtc, token)) is { } result ? Ok(result) : NotFound();

    [HttpPost("{id:guid}/tokens/{tokenId:guid}/revoke")]
    public async Task<IActionResult> RevokeToken(Guid id, Guid tokenId, CancellationToken token) =>
        await tokens.RevokeAsync(id, tokenId, token) ? NoContent() : NotFound();
}

public sealed record DashboardCommand(string Name, int TopCount = 10);
public sealed record TokenExpiryCommand(DateTimeOffset? ExpiresAtUtc);
public sealed record AdminDashboardResponse(Guid Id, string Name, int TopCount, bool IsEnabled, int ActiveTokenCount);
public sealed record DashboardTokenSummaryResponse(
    Guid TokenId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? LastUsedAtUtc);
