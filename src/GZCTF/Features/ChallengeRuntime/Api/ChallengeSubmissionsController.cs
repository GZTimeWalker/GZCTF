using System.Net.Mime;
using GZCTF.Features.ChallengeRuntime.Application;
using GZCTF.Middlewares;
using GZCTF.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Features.ChallengeRuntime.Api;

[RequireUser]
[ApiController]
[Route("api/challenges/{challengeId:guid}/submissions")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class ChallengeSubmissionsController(
    ChallengeSubmissionService submissions,
    AppDbContext db,
    UserManager<UserInfo> users,
    ChallengeAccessPolicy access) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ChallengeSubmissionResult>> Submit(
        Guid challengeId, [FromBody] ChallengeSubmissionRequest request, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user is null)
            return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Flag))
            return BadRequest();
        if (!await access.CanAccessAsync(challengeId, user, token)) return NotFound();
        return Ok(await submissions.SubmitAsync(user.Id, challengeId, request.Flag, token));
    }

    [HttpGet]
    public async Task<IActionResult> History(Guid challengeId, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user is null)
            return Unauthorized();
        if (!await access.CanAccessAsync(challengeId, user, token)) return NotFound();
        var history = await db.ChallengeSubmissions.AsNoTracking()
            .Where(item => item.UserId == user.Id && item.ChallengeId == challengeId)
            .OrderByDescending(item => item.SubmittedAtUtc)
            .Take(50)
            .Select(item => new
            {
                item.Id,
                item.Accepted,
                item.FirstSolve,
                item.SolveMode,
                item.RejectionCode,
                item.SubmittedAtUtc
            })
            .ToArrayAsync(token);
        return Ok(history);
    }
}

public sealed record ChallengeSubmissionRequest(string Flag);
