using System.Net.Mime;
using GZCTF.Features.ChallengeRuntime.Application;
using GZCTF.Features.ChallengeRuntime.Domain;
using GZCTF.Middlewares;
using GZCTF.Models;
using GZCTF.Models.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Features.ChallengeRuntime.Api;

[RequireUser]
[ApiController]
[Route("api/challenges/{challengeId:guid}/instances")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class ChallengeInstancesController(
    ChallengeRuntimeService runtime,
    UserManager<UserInfo> users) : ControllerBase
{
    [HttpGet]
    public Task<ActionResult<ChallengeInstanceResponse>> Get(Guid challengeId, CancellationToken token) =>
        Execute(challengeId, () => runtime.GetOwnedInstanceAsync(GetUserId(), challengeId, token));

    [HttpPost]
    public Task<ActionResult<ChallengeInstanceResponse>> Start(Guid challengeId, CancellationToken token) =>
        Execute(challengeId, () => runtime.StartAsync(GetUserId(), challengeId, token));

    [HttpPost("extend")]
    public Task<ActionResult<ChallengeInstanceResponse>> Extend(Guid challengeId, CancellationToken token) =>
        Execute(challengeId, () => runtime.ExtendAsync(GetUserId(), challengeId, token));

    [HttpDelete]
    public async Task<IActionResult> Stop(Guid challengeId, CancellationToken token)
    {
        try
        {
            await runtime.StopAsync(GetUserId(), challengeId, token);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    private async Task<ActionResult<ChallengeInstanceResponse>> Execute(
        Guid challengeId, Func<Task<UserChallengeInstance>> action)
    {
        try
        {
            return Ok(ToResponse(await action()));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    private Guid GetUserId() => Guid.TryParse(users.GetUserId(User), out var id)
        ? id
        : throw new UnauthorizedAccessException();

    private static ChallengeInstanceResponse ToResponse(UserChallengeInstance instance) =>
        new(instance.Id, instance.Status, instance.StartedAtUtc, instance.ExpiresAtUtc,
            null, null, instance.AssignedAttachmentKey, instance.AssignedAttachmentSha256);
}
