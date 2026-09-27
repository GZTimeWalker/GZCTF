using System.Net.Mime;
using GZCTF.Features.LearningPaths.Application;
using GZCTF.Models.Data;
using GZCTF.Middlewares;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Features.LearningPaths.Api;

[ApiController]
[Route("api/learning-lessons")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class LessonsController(
    EnrollmentService enrollmentService,
    LessonProgressService progressService,
    UserManager<UserInfo> userManager) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LessonContentResponse>> Get(
        Guid id, [FromQuery] string? locale, CancellationToken token)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Forbid();
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Forbid();

        try
        {
            return Ok(await enrollmentService.GetLessonAsync(user.Id, id, locale, token));
        }
        catch (LearningEnrollmentRequiredException)
        {
            return Forbid();
        }
        catch (LearningLessonNotFoundException)
        {
            return NotFound();
        }
    }

    [RequireUser]
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        try
        {
            await progressService.CompleteAsync(user.Id, id, token);
            return NoContent();
        }
        catch (LearningEnrollmentRequiredException)
        {
            return Forbid();
        }
        catch (LearningLessonNotFoundException)
        {
            return NotFound();
        }
    }
}
