using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.DataProtection;
using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using GZCTF.Middlewares;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Repositories.Interface;
using GZCTF.Storage.Interface;
using GZCTF.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Net.Http.Headers;

namespace GZCTF.Controllers;

/// <summary>
/// File APIs
/// </summary>
[ApiController]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class AssetsController(
    IBlobStorage storage,
    IBlobRepository blobService,
    ILogger<AssetsController> logger,
    AppDbContext context,
    IGameEventRepository eventRepository,
    IStringLocalizer<Program> localizer,
    IDataProtectionProvider dataProtectionProvider) : ControllerBase
{
    private readonly FileExtensionContentTypeProvider _extProvider = new();
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("GZCTF.Assets.Download");



    /// <summary>
    /// File retrieval interface
    /// </summary>
    /// <remarks>
    /// Retrieve a file by hash, filename is not matched
    /// </remarks>
    /// <param name="hash">File hash</param>
    /// <param name="filename">Download filename</param>
    /// <param name="token"></param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">File retrieved successfully</response>
    /// <response code="404">File not found</response>
    /// <response code="400">Failed to retrieve file</response>
    [HttpGet("[controller]/{hash:length(64)}/{filename:minlength(1)}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFile([RegularExpression("[0-9a-f]{64}")] string hash, string filename,
        [FromQuery] string? token, CancellationToken cancellationToken)
    {
        return await ServeFile(hash, filename, token, cancellationToken);
    }

    /// <summary>
    /// File retrieval interface with secure path token
    /// </summary>
    /// <param name="hash">File hash</param>
    /// <param name="token">Secure Token</param>
    /// <param name="filename">Download filename</param>
    /// <param name="cancellationToken"></param>
    [HttpGet("[controller]/{hash:length(64)}/s/{token}/{filename:minlength(1)}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFileWithToken([RegularExpression("[0-9a-f]{64}")] string hash, string token, string filename, CancellationToken cancellationToken)
    {
        return await ServeFile(hash, filename, token, cancellationToken);
    }

    private async Task<IActionResult> ServeFile(string hash, string filename, string? token, CancellationToken cancellationToken)
    {
        var path = StoragePath.Combine(PathHelper.Uploads, hash[..2], hash[2..4], hash);

        if (!await storage.ExistsAsync(path, cancellationToken))
        {
            var ip = HttpContext.Connection.RemoteIpAddress;
            logger.Log(StaticLocalizer[nameof(Resources.Program.Assets_FileNotFound), hash[..8], filename], ip,
                TaskStatus.NotFound,
                LogLevel.Warning);
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.File_NotFound)],
                StatusCodes.Status404NotFound));
        }

        if (!_extProvider.TryGetContentType(filename, out var contentType))
            contentType = MediaTypeNames.Application.Octet;

        // Log download (awaited to ensure DbContext is not disposed)
        await LogDownloadAsync(hash, User, token, cancellationToken);

        var blob = await storage.GetBlobAsync(path, cancellationToken);

        var stream = await storage.OpenReadAsync(path, cancellationToken);
        var etag = new EntityTagHeaderValue($"\"{hash[8..16]}\"");

        return File(stream, contentType, filename, blob.LastModificationTime, etag);
    }
    
    /// <summary>
    /// File upload interface
    /// </summary>
    /// <remarks>
    /// Upload one or more files
    /// </remarks>
    /// <param name="files"></param>
    /// <param name="filename">Unified filename</param>
    /// <param name="token"></param>
    /// <response code="200">File(s) uploaded successfully</response>
    /// <response code="400">Failed to upload file(s)</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Access denied</response>
    [RequireAdmin]
    [HttpPost("api/[controller]")]
    [ProducesResponseType(typeof(List<LocalFile>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> Upload(List<IFormFile> files, [FromQuery] string? filename,
        CancellationToken token)
    {
        try
        {
            List<LocalFile> results = [];
            foreach (var file in files.Where(file => file.Length > 0))
            {
                var res = await blobService.CreateOrUpdateBlob(file, filename, token);
                logger.SystemLog(
                    StaticLocalizer[nameof(Resources.Program.Assets_UpdateFile), res.Hash[..8],
                        filename ?? file.FileName, file.Length],
                    TaskStatus.Success, LogLevel.Debug);
                results.Add(res);
            }

            return Ok(results);
        }
        catch (Exception ex)
        {
            logger.LogErrorMessage(ex, ex.Message);
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Assets_IOError)]));
        }
    }

    /// <summary>
    /// File deletion interface
    /// </summary>
    /// <remarks>
    /// Delete a file by hash
    /// </remarks>
    /// <param name="hash"></param>
    /// <param name="token"></param>
    /// <response code="200">File deleted successfully</response>
    /// <response code="400">Failed to delete file</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Access denied</response>
    [RequireAdmin]
    [HttpDelete("api/[controller]/{hash:length(64)}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(string hash, CancellationToken token)
    {
        var result = await blobService.DeleteBlobByHash(hash, token);

        logger.SystemLog(StaticLocalizer[nameof(Resources.Program.Assets_DeleteFile), hash[..8]], result,
            LogLevel.Information);

        return result switch
        {
            TaskStatus.Success => Ok(),
            TaskStatus.NotFound => NotFound(),
            _ => BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_DeletionFailed)]))
        };
    }

    private sealed record DownloadTarget(
        int GameId,
        int ChallengeId,
        string ChallengeTitle,
        int? SourceTeamId,
        string? SourceTeamName);

    private sealed record SecureTokenContext(Guid? UserId, string? UserName, bool IsValid);

    private async Task LogDownloadAsync(string hash, ClaimsPrincipal? user, string? token, CancellationToken cancellationToken)
    {
        try
        {
            var actorUserId = await ResolveActorUserId(user, cancellationToken);
            var secureToken = await ParseSecureToken(token, hash, cancellationToken);

            var targets = await ResolveDownloadTargets(hash, cancellationToken);
            if (targets.Count == 0)
                return;

            var gameIds = targets.Select(t => t.GameId).Distinct().ToArray();

            var actorParticipations = await GetParticipationsByUser(gameIds, actorUserId, cancellationToken);
            var staticTokenParticipations = await GetParticipationsByStaticToken(gameIds, token, cancellationToken);
            var secureTokenParticipations = await GetParticipationsByUser(gameIds, secureToken.UserId, cancellationToken);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            foreach (var challengeGroup in targets.GroupBy(t => new { t.GameId, t.ChallengeId, t.ChallengeTitle }))
            {
                actorParticipations.TryGetValue(challengeGroup.Key.GameId, out var actorParticipation);

                staticTokenParticipations.TryGetValue(challengeGroup.Key.GameId, out var staticTokenParticipation);
                secureTokenParticipations.TryGetValue(challengeGroup.Key.GameId, out var secureTokenParticipation);
                var tokenParticipation = staticTokenParticipation ?? secureTokenParticipation;

                var selectedTarget = SelectRelevantTarget(challengeGroup, actorParticipation?.TeamId, tokenParticipation?.TeamId);

                var participation = actorParticipation ?? tokenParticipation;
                var teamId = participation?.TeamId ?? selectedTarget.SourceTeamId;
                var teamName = participation?.Team?.Name ?? selectedTarget.SourceTeamName ?? "Unknown";

                if (teamId is null)
                    continue;

                var eventUserId = actorUserId ?? secureToken.UserId;
                var downloadSource = BuildDownloadSource(user, actorUserId, token, secureToken);

                var sourceTeamId = tokenParticipation?.TeamId ?? selectedTarget.SourceTeamId;
                var sourceTeamName = tokenParticipation?.Team?.Name ?? selectedTarget.SourceTeamName ?? "Unknown";
                var abuseTag = string.Empty;

                if (!string.IsNullOrWhiteSpace(token) &&
                    actorParticipation is not null &&
                    sourceTeamId is not null &&
                    sourceTeamId != actorParticipation.TeamId)
                {
                    abuseTag = !string.IsNullOrWhiteSpace(secureToken.UserName)
                        ? $" [Token Source: {secureToken.UserName} (Team {sourceTeamName})]"
                        : $" [Token Source: Team {sourceTeamName}]";
                }

                await eventRepository.AddEvent(new GameEvent
                {
                    GameId = challengeGroup.Key.GameId,
                    TeamId = teamId.Value,
                    UserId = eventUserId,
                    Type = EventType.Download,
                    PublishTimeUtc = DateTimeOffset.UtcNow,
                    Values =
                    [
                        challengeGroup.Key.ChallengeId.ToString(),
                        "Attachment Download",
                        $"{downloadSource} from team {teamName} downloaded attachment for challenge {challengeGroup.Key.ChallengeTitle}.{abuseTag}",
                        ipAddress
                    ]
                }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to log download for hash {Hash}", hash);
        }
    }

    private async Task<Guid?> ResolveActorUserId(ClaimsPrincipal? user, CancellationToken cancellationToken)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        var claimUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(claimUserId, out var userId))
            return userId;

        var userName = user.Identity.Name;
        if (string.IsNullOrWhiteSpace(userName))
            return null;

        return await context.Users
            .Where(u => u.UserName == userName)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<SecureTokenContext> ParseSecureToken(string? token, string hash, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || !TryUnprotectTokenPayload(token, out var payload))
            return new(null, null, false);

        var parts = payload.Split('|');
        if (parts.Length != 4 || parts[0] != "v1")
            return new(null, null, false);

        if (!string.Equals(parts[1], hash, StringComparison.OrdinalIgnoreCase))
            return new(null, null, false);

        if (!long.TryParse(parts[3], out var expiryTicks) || DateTimeOffset.UtcNow.Ticks > expiryTicks)
            return new(null, null, false);

        if (!Guid.TryParse(parts[2], out var tokenUserId))
            return new(null, null, false);

        var tokenUserName = await context.Users
            .Where(u => u.Id == tokenUserId)
            .Select(u => u.UserName)
            .FirstOrDefaultAsync(cancellationToken);

        return new(tokenUserId, tokenUserName, true);
    }

    private bool TryUnprotectTokenPayload(string token, out string payload)
    {
        payload = string.Empty;

        try
        {
            var cipher = WebEncoders.Base64UrlDecode(token);
            payload = Encoding.UTF8.GetString(_protector.Unprotect(cipher));
            return true;
        }
        catch
        {
            // ignored
        }

        try
        {
            payload = _protector.Unprotect(token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<List<DownloadTarget>> ResolveDownloadTargets(string hash, CancellationToken cancellationToken)
    {
        var staticTargets = await context.GameChallenges
            .AsNoTracking()
            .Where(c => c.Attachment != null && c.Attachment.LocalFile != null && c.Attachment.LocalFile.Hash == hash)
            .Select(c => new DownloadTarget(c.GameId, c.Id, c.Title, null, null))
            .ToListAsync(cancellationToken);

        var dynamicTargets = await context.GameInstances
            .AsNoTracking()
            .Where(i => i.FlagContext != null
                        && i.FlagContext.Attachment != null
                        && i.FlagContext.Attachment.LocalFile != null
                        && i.FlagContext.Attachment.LocalFile.Hash == hash)
            .Select(i => new DownloadTarget(
                i.Challenge.GameId,
                i.ChallengeId,
                i.Challenge.Title,
                i.Participation.TeamId,
                i.Participation.Team.Name))
            .ToListAsync(cancellationToken);

        staticTargets.AddRange(dynamicTargets);
        return staticTargets;
    }

    private async Task<Dictionary<int, Participation>> GetParticipationsByUser(
        int[] gameIds,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        if (userId is null || gameIds.Length == 0)
            return [];

        var uid = userId.Value;
        var participations = await context.Participations
            .Include(p => p.Team)
            .Where(p => gameIds.Contains(p.GameId) && p.Members.Any(m => m.UserId == uid))
            .ToListAsync(cancellationToken);

        return participations
            .GroupBy(p => p.GameId)
            .ToDictionary(g => g.Key, g => g.First());
    }

    private async Task<Dictionary<int, Participation>> GetParticipationsByStaticToken(
        int[] gameIds,
        string? token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || gameIds.Length == 0)
            return [];

        var participations = await context.Participations
            .Include(p => p.Team)
            .Where(p => gameIds.Contains(p.GameId) && p.Token == token)
            .ToListAsync(cancellationToken);

        return participations
            .GroupBy(p => p.GameId)
            .ToDictionary(g => g.Key, g => g.First());
    }

    private static DownloadTarget SelectRelevantTarget(
        IEnumerable<DownloadTarget> targets,
        int? actorTeamId,
        int? tokenTeamId)
    {
        var targetList = targets.ToList();
        if (targetList.Count == 1)
            return targetList[0];

        if (actorTeamId is not null)
        {
            var actorTarget = targetList.FirstOrDefault(t => t.SourceTeamId == actorTeamId);
            if (actorTarget is not null)
                return actorTarget;
        }

        if (tokenTeamId is not null)
        {
            var tokenTarget = targetList.FirstOrDefault(t => t.SourceTeamId == tokenTeamId);
            if (tokenTarget is not null)
                return tokenTarget;
        }

        return targetList[0];
    }

    private static string BuildDownloadSource(
        ClaimsPrincipal? user,
        Guid? actorUserId,
        string? token,
        SecureTokenContext secureToken)
    {
        if (actorUserId is not null)
            return $"User {user?.Identity?.Name ?? "Unknown"}";

        if (secureToken.IsValid && secureToken.UserId is not null)
            return $"User {secureToken.UserName ?? "Unknown"} (via Secure Token)";

        if (!string.IsNullOrWhiteSpace(token))
            return "Team Member (via Static Token)";

        return "Anonymous";
    }
}
