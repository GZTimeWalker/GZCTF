using System.Net.Mime;
using GZCTF.Features.Shared;
using GZCTF.Features.SkillTrees.Application;
using GZCTF.Features.SkillTrees.Domain;
using GZCTF.Middlewares;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Features.SkillTrees.Api;

[RequireAdmin]
[ApiController]
[Route("api/admin/skill-trees")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class AdminSkillTreesController(AdminSkillTreeService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminSkillTreeResponse>>> List(CancellationToken token)
    {
        var trees = await service.ListAdminAsync(token);
        return Ok(trees);
    }

    [HttpPost]
    public async Task<ActionResult<AdminSkillTreeResponse>> Create(
        [FromBody] CreateSkillTreeCommand command, CancellationToken token)
    {
        var result = await service.CreateAsync(command, token);
        return Ok(result);
    }

    [HttpGet("{id:guid}/draft")]
    public async Task<ActionResult<SkillTreeDraftResponse>> GetDraft(
        Guid id, CancellationToken token)
    {
        var result = await service.GetDraftAsync(id, token);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:guid}/draft/preview")]
    public async Task<ActionResult<SkillTreeDetailResponse>> PreviewDraft(
        Guid id, CancellationToken token)
    {
        var result = await service.GetDraftPreviewAsync(id, token);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:guid}/draft")]
    public async Task<ActionResult<SkillTreeDraftResponse>> UpdateDraft(
        Guid id, [FromBody] UpdateSkillTreeDraftCommand command, CancellationToken token)
    {
        try
        {
            var result = await service.UpdateDraftAsync(id, command, token);
            return result is null ? NotFound() : Ok(result);
        }
        catch (SkillTreeRevisionConflictException)
        {
            return Conflict(ApiError.Conflict(
                SkillTreeProblemCodes.RevisionConflict,
                "The skill tree draft changed since it was loaded.",
                HttpContext.TraceIdentifier));
        }
        catch (SkillTreeValidationException ex)
        {
            return BadRequest(ApiError.Validation(
                "skill_tree_invalid_draft", ex.Message, HttpContext.TraceIdentifier, null));
        }
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(
        Guid id, [FromBody] PublishSkillTreeCommand command, CancellationToken token)
    {
        try
        {
            await service.PublishAsync(id, command.RowVersion, token);
            return NoContent();
        }
        catch (SkillTreeNotFoundException)
        {
            return NotFound();
        }
        catch (SkillTreeRevisionConflictException)
        {
            return Conflict(ApiError.Conflict(
                SkillTreeProblemCodes.RevisionConflict,
                "The skill tree was changed by another administrator.",
                HttpContext.TraceIdentifier));
        }
        catch (SkillTreeValidationException ex)
        {
            return BadRequest(ApiError.Validation(
                "skill_tree_invalid_draft", ex.Message, HttpContext.TraceIdentifier, null));
        }
    }

    [HttpGet("{id:guid}/delete-impact")]
    public async Task<ActionResult<SkillTreeDeleteImpactResponse>> GetDeleteImpact(
        Guid id, CancellationToken token)
    {
        var result = await service.GetDeleteImpactAsync(id, token);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id, [FromBody] DeleteSkillTreeCommand command, CancellationToken token)
    {
        try
        {
            await service.DeleteAsync(id, command, token);
            return NoContent();
        }
        catch (SkillTreeNotFoundException)
        {
            return NotFound();
        }
        catch (SkillTreeRevisionConflictException)
        {
            return Conflict(ApiError.Conflict(
                SkillTreeProblemCodes.RevisionConflict,
                "The skill tree was changed by another administrator.",
                HttpContext.TraceIdentifier));
        }
        catch (SkillTreeConfirmationMismatchException)
        {
            return BadRequest(ApiError.Validation(
                SkillTreeProblemCodes.ConfirmationMismatch,
                "The confirmation name does not match the skill tree name.",
                HttpContext.TraceIdentifier, null));
        }
    }
}
