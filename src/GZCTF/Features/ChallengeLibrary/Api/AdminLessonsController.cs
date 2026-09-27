using System.Net.Mime;
using GZCTF.Features.ChallengeLibrary.Application;
using GZCTF.Features.Shared;
using GZCTF.Features.SkillTrees.Application;
using GZCTF.Middlewares;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Features.ChallengeLibrary.Api;

[RequireAdmin]
[ApiController]
[Route("api/admin/lessons")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class AdminLessonsController(
    ChallengeLibraryService service,
    ContentPublicationService publicationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LessonResponse>>> List(
        [FromQuery] string? locale, CancellationToken token) =>
        Ok(await service.ListLessonsAsync(locale, token));

    [HttpPost]
    public async Task<ActionResult<LessonResponse>> Create(
        [FromBody] LessonCommand command, CancellationToken token) =>
        Ok(await service.CreateLessonAsync(command, token));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LessonResponse>> Get(
        Guid id, [FromQuery] string? locale, CancellationToken token)
    {
        var response = await service.GetLessonAsync(id, locale, token);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LessonResponse>> Update(
        Guid id, [FromBody] LessonCommand command, CancellationToken token)
    {
        var response = await service.UpdateLessonAsync(id, command, token);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token) =>
        await service.DeleteLessonAsync(id, token) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(
        Guid id, [FromBody] PublishContentCommand command, CancellationToken token)
    {
        try
        {
            await publicationService.PublishLessonAsync(id, command, token);
            return NoContent();
        }
        catch (ContentCategoryRequiredException)
        {
            return BadRequest(ApiError.Validation(
                SkillTreeProblemCodes.ContentCategoryRequired,
                "At least one category is required before publication.",
                HttpContext.TraceIdentifier, null));
        }
        catch (ContentCategoryHasNoTreeException)
        {
            return BadRequest(ApiError.Validation(
                SkillTreeProblemCodes.CategoryHasNoTree,
                "The selected category does not belong to any active skill tree.",
                HttpContext.TraceIdentifier, null));
        }
        catch (SkillTreeRevisionConflictException)
        {
            return Conflict(ApiError.Conflict(
                SkillTreeProblemCodes.RevisionConflict,
                "The content was changed by another administrator.",
                HttpContext.TraceIdentifier));
        }
        catch (ContentPublicationValidationException exception)
        {
            return BadRequest(ApiError.Validation(
                "content_invalid_publication", exception.Message,
                HttpContext.TraceIdentifier, null));
        }
    }
}
