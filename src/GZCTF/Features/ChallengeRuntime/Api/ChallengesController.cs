using System.Net.Mime;
using GZCTF.Features.ChallengeRuntime.Application;
using GZCTF.Features.ChallengeRuntime.Infrastructure;
using GZCTF.Middlewares;
using GZCTF.Models;
using GZCTF.Models.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GZCTF.Utils;

namespace GZCTF.Features.ChallengeRuntime.Api;

[RequireUser]
[ApiController]
[Route("api/challenges")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class ChallengesController(
    AppDbContext db,
    UserManager<UserInfo> users,
    ChallengeRuntimeService runtime,
    DynamicAttachmentAllocator attachments,
    ILegacyStorageAdapter storage,
    ChallengeHelpService help) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChallengeRuntimeDetailResponse>> Get(
        Guid id, [FromQuery] string? locale, CancellationToken token)
    {
        var challenge = await db.Challenges.AsNoTracking()
            .Include(item => item.Localizations)
            .Include(item => item.Hints)
            .Include(item => item.Writeups)
            .SingleOrDefaultAsync(item => item.Id == id, token);
        if (challenge is null)
            return NotFound();
        var text = challenge.Localizations.FirstOrDefault(item =>
                       string.Equals(item.Locale, locale, StringComparison.OrdinalIgnoreCase)) ??
                   challenge.Localizations.FirstOrDefault(item => item.Locale == "en");
        return Ok(new ChallengeRuntimeDetailResponse(
            challenge.Id,
            text?.Title ?? string.Empty,
            text?.Summary ?? string.Empty,
            text?.Body ?? string.Empty,
            challenge.Type.ToString(),
            challenge.Hints.Select(item => item.Locale).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            challenge.Writeups.Count > 0,
            challenge.Type.IsAttachment(),
            challenge.Type.IsContainer()));
    }

    [HttpGet("{id:guid}/attachment")]
    public async Task<IActionResult> DownloadAttachment(Guid id, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user is null)
            return Unauthorized();
        var instance = await runtime.GetOrCreateInstanceAsync(user.Id, id, token);
        var assignment = await attachments.GetOrAllocateAsync(user.Id, id, token);
        if (!await storage.ExistsAsync(assignment.FileName, token))
            return NotFound();
        var stream = await storage.OpenReadAsync(assignment.FileName, token);
        return File(stream, MediaTypeNames.Application.Octet, assignment.FileName);
    }

    [HttpGet("{id:guid}/hints/next")]
    public async Task<ActionResult<ChallengeHintResponse>> NextHint(
        Guid id, [FromQuery] string? locale, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user is null)
            return Unauthorized();
        var response = await help.RevealNextHintAsync(user.Id, id, locale, token);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("{id:guid}/writeup")]
    public async Task<ActionResult<ChallengeWriteupResponse>> Writeup(
        Guid id, [FromQuery] string? locale, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user is null)
            return Unauthorized();
        var response = await help.RevealWriteupAsync(user.Id, id, locale, token);
        return response is null ? NotFound() : Ok(response);
    }
}

public sealed record ChallengeRuntimeDetailResponse(
    Guid Id,
    string Title,
    string Summary,
    string Body,
    string Type,
    int HintLocaleCount,
    bool HasWriteup,
    bool HasAttachment,
    bool HasContainer);
