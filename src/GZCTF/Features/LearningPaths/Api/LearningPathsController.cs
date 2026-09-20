using System.Net.Mime;
using GZCTF.Features.LearningPaths.Application;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Features.LearningPaths.Api;

[ApiController]
[Route("api/learning-paths")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class LearningPathsController(LearningPathService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LearningPathSummaryResponse>>> List(
        [FromQuery] string? locale, CancellationToken token) =>
        Ok(await service.ListPublishedAsync(locale, token));

    [HttpGet("{slug}/preview")]
    public async Task<ActionResult<LearningPathPreviewResponse>> Preview(
        string slug, [FromQuery] string? locale, CancellationToken token)
    {
        var response = await service.GetPublishedPreviewAsync(slug, locale, token);
        return response is null ? NotFound() : Ok(response);
    }
}
