using System.Net.Mime;
using GZCTF.Features.SkillTrees.Application;
using GZCTF.Middlewares;
using GZCTF.Models.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Features.SkillTrees.Api;

[RequireUser]
[ApiController]
[Route("api/skill-tree-enrollments")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class SkillTreeEnrollmentsController(
    SkillTreeEnrollmentService service,
    UserManager<UserInfo> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SkillTreeEnrollmentResponse>>> List(CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        return user is null ? Unauthorized() : Ok(await service.ListAsync(user.Id, token));
    }

    [HttpPost("{id:guid}")]
    public async Task<ActionResult<SkillTreeEnrollmentResponse>> Enroll(Guid id, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();
        var response = await service.EnrollAsync(user.Id, id, token);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Leave(Guid id, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();
        return await service.LeaveAsync(user.Id, id, token) ? NoContent() : NotFound();
    }

    [HttpPut("{id:guid}/current")]
    public async Task<ActionResult<SkillTreeEnrollmentResponse>> SelectCurrent(Guid id, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();
        var response = await service.SelectCurrentAsync(user.Id, id, token);
        return response is null ? NotFound() : Ok(response);
    }
}
