using System.Net.Mime;
using GZCTF.Features.ChallengeRuntime.Application;
using GZCTF.Features.ChallengeRuntime.Domain;
using GZCTF.Middlewares;
using GZCTF.Models;
using GZCTF.Models.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.ChallengeRuntime.Api;

[RequireUser]
[ApiController]
[Route("api/challenges/{challengeId:guid}/instances")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class ChallengeInstancesController(
    ChallengeRuntimeService runtime,
    UserManager<UserInfo> users,
    ChallengeAccessPolicy access,
    AppDbContext db) : ControllerBase
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
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        if (!await access.CanAccessAsync(challengeId, user, token)) return NotFound();
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
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        if (!await access.CanAccessAsync(challengeId, user, HttpContext.RequestAborted)) return NotFound();
        try
        {
            return Ok(await ToResponseAsync(await action(), HttpContext.RequestAborted));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    private Guid GetUserId() => Guid.TryParse(users.GetUserId(User), out var id)
        ? id
        : throw new UnauthorizedAccessException();

    private async Task<ChallengeInstanceResponse> ToResponseAsync(
        UserChallengeInstance instance, CancellationToken token)
    {
        var container = instance.ContainerId is { } containerId
            ? await db.Containers.AsNoTracking().FirstOrDefaultAsync(item => item.Id == containerId, token)
            : null;
        var host = container is null ? null : container.IsProxy
            ? container.Entry : container.PublicIP ?? container.IP;
        int? port = container is null || container.IsProxy
            ? null : container.PublicPort ?? container.Port;
        return new ChallengeInstanceResponse(
            instance.Id, instance.Status, instance.StartedAtUtc, instance.ExpiresAtUtc,
            host, port, instance.AssignedAttachmentKey, instance.AssignedAttachmentSha256);
    }
}
