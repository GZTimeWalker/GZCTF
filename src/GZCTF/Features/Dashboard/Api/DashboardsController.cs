using GZCTF.Features.Dashboard.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Features.Dashboard.Api;

[AllowAnonymous]
[ApiController]
[Route("api/dashboards")]
public sealed class DashboardsController(DashboardSnapshotService snapshots, DashboardTokenService tokens) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DashboardSnapshotResponse>> Get(
        Guid id, [FromQuery] Guid? cohortId, [FromQuery] string? search, CancellationToken token)
    {
        if (!await tokens.IsValidAsync(id, Request.Query["token"].FirstOrDefault(), HttpContext.Connection.RemoteIpAddress?.ToString(), token))
            return Unauthorized();
        var response = await snapshots.GetAsync(id, cohortId, search, token);
        return response is null ? NotFound() : Ok(response);
    }
}
