using GZCTF.Features.Dashboard.Application;
using GZCTF.Middlewares;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Features.Dashboard.Api;

[RequireAdmin]
[ApiController]
[Route("api/admin/dashboard/projection")]
public sealed class AdminDashboardProjectionController(DailySolveProjection projection) : ControllerBase
{
    [HttpPost("rebuild")]
    public async Task<ActionResult<DailySolveRebuildResult>> Rebuild(CancellationToken token) =>
        Ok(await projection.RebuildAsync(token));
}
