using System.Net.Mime;
using GZCTF.Features.ChallengeLibrary.Application;
using GZCTF.Features.Shared;
using GZCTF.Features.SkillTrees.Application;
using GZCTF.Middlewares;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Features.ChallengeLibrary.Api;

[RequireAdmin]
[ApiController]
[Route("api/admin/challenges")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class AdminChallengesController(
    ChallengeLibraryService service,
    ContentPublicationService publicationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ChallengeSummaryResponse>>> List(
        [FromQuery] string? locale, CancellationToken token) =>
        Ok(await service.ListChallengesAsync(locale, token));

    [HttpPost]
    public async Task<ActionResult<ChallengeEditResponse>> Create(
        [FromBody] ChallengeCommand command, CancellationToken token)
    {
        try
        {
            return Ok(await service.CreateChallengeAsync(command, token));
        }
        catch (ChallengeValidationException exception)
        {
            return BadRequest(ApiError.Validation("learning.challenge_invalid", exception.Message,
                HttpContext.TraceIdentifier, null));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChallengeSummaryResponse>> Get(
        Guid id, [FromQuery] string? locale, CancellationToken token)
    {
        var response = await service.GetChallengeAsync(id, locale, token);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("{id:guid}/edit")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<ActionResult<ChallengeEditResponse>> GetForEdit(
        Guid id, [FromQuery] string? locale, CancellationToken token)
    {
        var response = await service.GetChallengeForEditAsync(id, locale, token);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ChallengeEditResponse>> Update(
        Guid id, [FromBody] ChallengeCommand command, CancellationToken token)
    {
        try
        {
            var response = await service.UpdateChallengeAsync(id, command, token);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ChallengeTypeImmutableException)
        {
            return Conflict(ApiError.Conflict(
                "learning.challenge_type_immutable",
                "A published challenge cannot change type.",
                HttpContext.TraceIdentifier));
        }
        catch (ChallengeRevisionConflictException)
        {
            return Conflict(ApiError.Conflict(
                "learning.challenge_changed",
                "The challenge changed since it was loaded.",
                HttpContext.TraceIdentifier));
        }
        catch (ChallengeValidationException exception)
        {
            return BadRequest(ApiError.Validation("learning.challenge_invalid", exception.Message,
                HttpContext.TraceIdentifier, null));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token) =>
        await service.RetireChallengeAsync(id, token) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(
        Guid id, [FromBody] PublishContentCommand command, CancellationToken token)
    {
        try
        {
            await publicationService.PublishChallengeAsync(id, command, token);
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
                exception.Code, exception.Message,
                HttpContext.TraceIdentifier, null));
        }
    }

    [HttpPost("{id:guid}/merge")]
    public async Task<ActionResult<ChallengeMergeResult>> Merge(
        Guid id, [FromBody] MergeChallengeCommand command, CancellationToken token)
    {
        try
        {
            return Ok(await service.MergeChallengesAsync(id, command.SurvivorId, token));
        }
        catch (ChallengeMergeConflictException)
        {
            return Conflict(ApiError.Conflict(
                "learning.challenge_merge_conflict",
                "The challenge has an active runtime instance and cannot be merged.",
                HttpContext.TraceIdentifier));
        }
        catch (ChallengeMergeException exception)
        {
            return BadRequest(ApiError.Conflict(
                "learning.challenge_merge_invalid",
                exception.Message,
                HttpContext.TraceIdentifier));
        }
    }
}

public sealed class MergeChallengeCommand
{
    public Guid SurvivorId { get; set; }
}
