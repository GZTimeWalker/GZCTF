using System.Net.Mime;
using GZCTF.Features.LearningPaths.Application;
using GZCTF.Middlewares;
using GZCTF.Models.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Features.LearningPaths.Api;

[RequireUser]
[ApiController]
[Route("api/learning-paths")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class EnrollmentsController(
    EnrollmentService service,
    UserManager<UserInfo> userManager) : ControllerBase
{
    [HttpGet("enrollments")]
    public async Task<ActionResult<IReadOnlyList<EnrollmentResponse>>> List(
        [FromQuery] string? locale, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        return user is null ? Unauthorized() : Ok(await service.ListAsync(user.Id, locale, token));
    }

    [HttpPost("{pathId:guid}/enroll")]
    public async Task<ActionResult<EnrollmentResponse>> Enroll(
        Guid pathId, [FromQuery] string? locale, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();
        var response = await service.EnrollAsync(user.Id, pathId, locale, token);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpDelete("{pathId:guid}/enroll")]
    public async Task<IActionResult> Leave(Guid pathId, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();
        return await service.LeaveAsync(user.Id, pathId, token) ? NoContent() : NotFound();
    }

    [HttpPost("{pathId:guid}/select")]
    public async Task<ActionResult<EnrollmentResponse>> Select(
        Guid pathId, [FromQuery] string? locale, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();
        var response = await service.SelectCurrentAsync(user.Id, pathId, locale, token);
        return response is null ? NotFound() : Ok(response);
    }
}
