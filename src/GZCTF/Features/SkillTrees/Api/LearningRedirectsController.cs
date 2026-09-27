using GZCTF.Features.SkillTrees.Application;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Features.SkillTrees.Api;

[ApiController]
[Route("api/skill-tree-redirects")]
public sealed class LearningRedirectsController(LearningRedirectService service) : ControllerBase
{
    [HttpGet("{slug}")]
    public async Task<ActionResult<LearningRedirectResponse>> Get(
        string slug,
        [FromQuery] string? moduleId,
        [FromQuery] string? itemId,
        CancellationToken token)
    {
        var target = await service.ResolveAsync(slug, moduleId, itemId, token);
        return target is null ? NotFound() : Ok(new LearningRedirectResponse(target));
    }
}
