using System.Net.Mime;
using GZCTF.Features.LearningProgress.Application;
using GZCTF.Middlewares;
using GZCTF.Models.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Features.LearningProgress.Api;

[RequireUser]
[ApiController]
[Route("api/my-learning")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class MyLearningController(
    LearningRecordService service,
    UserManager<UserInfo> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MyLearningResponse>> Get(
        [FromQuery] string? locale, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        return user is null ? Unauthorized() : Ok(await service.GetAsync(user.Id, locale, token));
    }
}
