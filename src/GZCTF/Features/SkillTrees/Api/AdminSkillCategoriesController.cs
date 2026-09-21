using System.Net.Mime;
using GZCTF.Features.Shared;
using GZCTF.Features.SkillTrees.Application;
using GZCTF.Middlewares;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Features.SkillTrees.Api;

[RequireAdmin]
[ApiController]
[Route("api/admin/skill-categories")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class AdminSkillCategoriesController(SkillCategoryService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SkillCategoryAdminResponse>>> List(CancellationToken token) =>
        Ok(await service.ListAsync(token));

    [HttpPost]
    public async Task<ActionResult<SkillCategoryAdminResponse>> Create(
        [FromBody] SkillCategoryCommand command, CancellationToken token)
    {
        try
        {
            return Ok(await service.CreateAsync(command, token));
        }
        catch (SkillTreeInvalidIconException)
        {
            return InvalidIcon();
        }
        catch (SkillCategoryValidationException exception)
        {
            return Invalid(exception.Message);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SkillCategoryAdminResponse>> Get(Guid id, CancellationToken token) =>
        await service.GetAsync(id, token) is { } category ? Ok(category) : NotFound();

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SkillCategoryAdminResponse>> Update(
        Guid id, [FromBody] SkillCategoryCommand command, CancellationToken token)
    {
        try
        {
            var result = await service.UpdateAsync(id, command, token);
            return result is null ? NotFound() : Ok(result);
        }
        catch (SkillTreeInvalidIconException)
        {
            return InvalidIcon();
        }
        catch (SkillCategoryValidationException exception)
        {
            return Invalid(exception.Message);
        }
        catch (SkillTreeRevisionConflictException)
        {
            return Conflict(ApiError.Conflict(
                SkillTreeProblemCodes.RevisionConflict,
                "The category was changed by another administrator.",
                HttpContext.TraceIdentifier));
        }
    }

    [HttpPut("{id:guid}/contents")]
    public async Task<ActionResult<SkillCategoryAdminResponse>> UpdateContents(
        Guid id, [FromBody] UpdateCategoryContentsCommand command, CancellationToken token)
    {
        try
        {
            var result = await service.UpdateContentsAsync(id, command, token);
            return result is null ? NotFound() : Ok(result);
        }
        catch (SkillCategoryValidationException exception)
        {
            return Invalid(exception.Message);
        }
        catch (SkillTreeRevisionConflictException)
        {
            return Conflict(ApiError.Conflict(
                SkillTreeProblemCodes.RevisionConflict,
                "The category was changed by another administrator.",
                HttpContext.TraceIdentifier));
        }
    }

    [HttpPut("{id:guid}/tree-memberships")]
    public async Task<ActionResult<UpdateCategoryTreeMembershipsResponse>> UpdateTreeMemberships(
        Guid id, [FromBody] UpdateCategoryTreeMembershipsCommand command, CancellationToken token)
    {
        try
        {
            var result = await service.UpdateTreeMembershipsAsync(id, command, token);
            return result is null ? NotFound() : Ok(result);
        }
        catch (SkillCategoryValidationException exception)
        {
            return Invalid(exception.Message);
        }
        catch (SkillTreeRevisionConflictException)
        {
            return Conflict(ApiError.Conflict(
                SkillTreeProblemCodes.RevisionConflict,
                "A referenced skill tree was changed by another administrator.",
                HttpContext.TraceIdentifier));
        }
    }

    [HttpGet("{id:guid}/delete-impact")]
    public async Task<ActionResult<CategoryDeleteImpactResponse>> GetDeleteImpact(
        Guid id, CancellationToken token) =>
        await service.GetDeleteImpactAsync(id, token) is { } impact ? Ok(impact) : NotFound();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id, [FromBody] DeleteCategoryCommand command, CancellationToken token)
    {
        try
        {
            await service.DeleteAsync(id, command, token);
            return NoContent();
        }
        catch (SkillCategoryNotFoundException)
        {
            return NotFound();
        }
        catch (SkillCategoryConfirmationMismatchException)
        {
            return Invalid("The confirmation name does not match the category name.",
                SkillTreeProblemCodes.ConfirmationMismatch);
        }
        catch (SkillTreeRevisionConflictException)
        {
            return Conflict(ApiError.Conflict(
                SkillTreeProblemCodes.RevisionConflict,
                "The category was changed by another administrator.",
                HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("merge")]
    public async Task<IActionResult> Merge(
        [FromBody] MergeSkillCategoryCommand command, CancellationToken token)
    {
        try
        {
            await service.MergeAsync(command, token);
            return NoContent();
        }
        catch (SkillTreeRevisionConflictException)
        {
            return Conflict(ApiError.Conflict(
                SkillTreeProblemCodes.RevisionConflict,
                "A category was changed by another administrator.",
                HttpContext.TraceIdentifier));
        }
        catch (SkillCategoryMergeConflictException exception)
        {
            return Conflict(ApiError.Conflict(
                SkillTreeProblemCodes.MergeConflict,
                exception.Message,
                HttpContext.TraceIdentifier));
        }
        catch (SkillCategoryValidationException exception)
        {
            return Invalid(exception.Message);
        }
    }

    private BadRequestObjectResult Invalid(string message, string code = "skill_category_invalid") =>
        BadRequest(ApiError.Validation(code, message, HttpContext.TraceIdentifier, null));

    private BadRequestObjectResult InvalidIcon() =>
        Invalid("The icon key is not supported.", SkillTreeProblemCodes.InvalidIcon);
}
