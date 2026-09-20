using System.Net.Mime;
using GZCTF.Features.LearningPaths.Application;
using GZCTF.Features.Shared;
using GZCTF.Middlewares;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Features.LearningPaths.Api;

[RequireAdmin]
[ApiController]
[Route("api/admin/learning-paths")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class AdminLearningPathsController(LearningPathService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<LearningPathDraftResponse>> Create(
        [FromBody] LearningPathCommand command, CancellationToken token) =>
        Ok(await service.CreatePathAsync(command, token));

    [HttpGet("{id:guid}/draft")]
    public async Task<ActionResult<LearningPathDraftResponse>> GetDraft(
        Guid id, [FromQuery] string? locale, CancellationToken token)
    {
        var response = await service.GetDraftAsync(id, locale, token);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPut("{id:guid}/draft")]
    public async Task<ActionResult<LearningPathDraftResponse>> UpdateDraft(
        Guid id, [FromBody] LearningPathCommand command, CancellationToken token)
    {
        try
        {
            var response = await service.UpdateDraftAsync(id, command, token);
            return response is null ? NotFound() : Ok(response);
        }
        catch (LearningRevisionConflictException)
        {
            return Conflict(ApiError.Conflict(
                "learning.revision_conflict",
                "The learning path draft changed since it was loaded.",
                HttpContext.TraceIdentifier));
        }
    }

    [HttpGet("{id:guid}/draft/preview")]
    public async Task<ActionResult<LearningPathPreviewResponse>> PreviewDraft(
        Guid id, [FromQuery] string? locale, CancellationToken token)
    {
        var response = await service.GetDraftPreviewAsync(id, locale, token);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(
        Guid id, [FromBody] PublishLearningPathCommand command, CancellationToken token)
    {
        try
        {
            await service.PublishAsync(id, command.RowVersion, token);
            return NoContent();
        }
        catch (LearningRevisionConflictException)
        {
            return Conflict(ApiError.Conflict(
                "learning.revision_conflict",
                "The learning path draft changed since it was loaded.",
                HttpContext.TraceIdentifier));
        }
        catch (LearningPathNotFoundException)
        {
            return NotFound();
        }
        catch (LearningPathValidationException exception)
        {
            return BadRequest(ApiError.Conflict(
                "learning.invalid_draft", exception.Message, HttpContext.TraceIdentifier));
        }
    }
}

public sealed record PublishLearningPathCommand(uint? RowVersion);
