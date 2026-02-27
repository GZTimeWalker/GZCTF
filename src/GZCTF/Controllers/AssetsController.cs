using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.DataProtection;
using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using GZCTF.Middlewares;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Internal;
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
        return await ServeFile(hash, filename, token, requireValidSecureToken: false, cancellationToken);
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
        return await ServeFile(hash, filename, token, requireValidSecureToken: true, cancellationToken);
    }

    /// <summary>
    /// Remote attachment retrieval interface
    /// </summary>
    /// <param name="attachmentId">Attachment ID</param>
    /// <param name="filename">Download filename</param>
    /// <param name="token"></param>
    /// <param name="cancellationToken"></param>
    [HttpGet("[controller]/remote/{attachmentId:int}/{filename:minlength(1)}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRemoteFile(int attachmentId, string filename, [FromQuery] string? token, CancellationToken cancellationToken)
    {
        return await ServeRemoteFile(attachmentId, filename, token, requireValidSecureToken: true, cancellationToken);
    }

    /// <summary>
    /// Remote attachment retrieval interface with secure path token
    /// </summary>
    /// <param name="attachmentId">Attachment ID</param>
    /// <param name="token">Secure Token</param>
    /// <param name="filename">Download filename</param>
    /// <param name="cancellationToken"></param>
    [HttpGet("[controller]/remote/{attachmentId:int}/s/{token}/{filename:minlength(1)}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRemoteFileWithToken(int attachmentId, string token, string filename, CancellationToken cancellationToken)
    {
        return await ServeRemoteFile(attachmentId, filename, token, requireValidSecureToken: true, cancellationToken);
    }

    private async Task<IActionResult> ServeFile(
        string hash,
        string filename,
        string? token,
        bool requireValidSecureToken,
        CancellationToken cancellationToken)
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

        var accessContext = await BuildDownloadAccessContextByHash(hash, User, token, cancellationToken);
        if (!IsDownloadAllowed(accessContext, User, requireValidSecureToken))
            return Forbid();

        if (!_extProvider.TryGetContentType(filename, out var contentType))
            contentType = MediaTypeNames.Application.Octet;

        // Log download (awaited to ensure DbContext is not disposed)
        await LogDownloadAsync(accessContext, User, token, cancellationToken);

        var blob = await storage.GetBlobAsync(path, cancellationToken);

        var stream = await storage.OpenReadAsync(path, cancellationToken);
        var etag = new EntityTagHeaderValue($"\"{hash[8..16]}\"");

        return File(stream, contentType, filename, blob.LastModificationTime, etag);
    }

    private async Task<IActionResult> ServeRemoteFile(
        int attachmentId,
        string filename,
        string? token,
        bool requireValidSecureToken,
        CancellationToken cancellationToken)
    {
        var remoteAttachment = await context.Attachments
            .AsNoTracking()
            .Where(a => a.Id == attachmentId && a.Type == FileType.Remote && !string.IsNullOrWhiteSpace(a.RemoteUrl))
            .Select(a => a.RemoteUrl)
            .SingleOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(remoteAttachment))
        {
            var ip = HttpContext.Connection.RemoteIpAddress;
            logger.Log(StaticLocalizer[nameof(Resources.Program.Assets_FileNotFound), $"remote:{attachmentId}", filename], ip,
                TaskStatus.NotFound,
                LogLevel.Warning);
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.File_NotFound)],
                StatusCodes.Status404NotFound));
        }

        if (!Uri.TryCreate(remoteAttachment, UriKind.Absolute, out var remoteUri) ||
            (remoteUri.Scheme != Uri.UriSchemeHttp && remoteUri.Scheme != Uri.UriSchemeHttps))
        {
            logger.LogWarning("Invalid remote attachment URL for attachment {AttachmentId}: {RemoteUrl}", attachmentId, remoteAttachment);
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Assets_IOError)]));
        }

        var accessContext = await BuildDownloadAccessContextByAttachmentId(attachmentId, User, token, cancellationToken);
        if (accessContext.Targets.Count == 0)
        {
            logger.LogWarning("Remote attachment {AttachmentId} has no authorized challenge target", attachmentId);
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.File_NotFound)],
                StatusCodes.Status404NotFound));
        }

        if (!IsDownloadAllowed(accessContext, User, requireValidSecureToken))
            return Forbid();

        await LogDownloadAsync(accessContext, User, token, cancellationToken);

        return Redirect(remoteAttachment);
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

    private sealed record DownloadAccessContext(
        string TargetIdentifier,
        Guid? ActorUserId,
        SecureTokenContext SecureToken,
        List<DownloadTarget> Targets,
        Dictionary<int, Participation> ActorParticipations,
        Dictionary<int, Participation> StaticTokenParticipations,
        Dictionary<int, Participation> SecureTokenParticipations);

    private async Task<DownloadAccessContext> BuildDownloadAccessContextByHash(
        string hash,
        ClaimsPrincipal? user,
        string? token,
        CancellationToken cancellationToken)
    {
        return await BuildDownloadAccessContextCore(
            DownloadTokenTarget.ForLocalHash(hash),
            user,
            token,
            ResolveDownloadTargetsByHash,
            hash,
            cancellationToken);
    }

    private async Task<DownloadAccessContext> BuildDownloadAccessContextByAttachmentId(
        int attachmentId,
        ClaimsPrincipal? user,
        string? token,
        CancellationToken cancellationToken)
    {
        return await BuildDownloadAccessContextCore(
            DownloadTokenTarget.ForRemoteAttachment(attachmentId),
            user,
            token,
            ResolveDownloadTargetsByAttachmentId,
            attachmentId,
            cancellationToken);
    }

    private async Task<DownloadAccessContext> BuildDownloadAccessContextCore<TTarget>(
        string targetIdentifier,
        ClaimsPrincipal? user,
        string? token,
        Func<TTarget, CancellationToken, Task<List<DownloadTarget>>> targetResolver,
        TTarget targetValue,
        CancellationToken cancellationToken)
    {
        var actorUserId = await ResolveActorUserId(user, cancellationToken);
        var secureToken = await ParseSecureToken(token, targetIdentifier, cancellationToken);
        var targets = await targetResolver(targetValue, cancellationToken);

        if (targets.Count == 0)
            return new(targetIdentifier, actorUserId, secureToken, targets, [], [], []);

        var gameIds = targets.Select(t => t.GameId).Distinct().ToArray();
        var actorParticipations = await GetParticipationsByUser(gameIds, actorUserId, cancellationToken);
        var staticTokenParticipations = await GetParticipationsByStaticToken(gameIds, token, cancellationToken);
        var secureTokenParticipations = await GetParticipationsByUser(gameIds, secureToken.UserId, cancellationToken);

        return new(
            targetIdentifier,
            actorUserId,
            secureToken,
            targets,
            actorParticipations,
            staticTokenParticipations,
            secureTokenParticipations);
    }

    private static bool IsDownloadAllowed(
        DownloadAccessContext accessContext,
        ClaimsPrincipal? user,
        bool requireValidSecureToken)
    {
        if (accessContext.Targets.Count == 0)
            return true;

        if (user?.IsInRole(Role.Admin.ToString()) == true)
            return true;

        if (requireValidSecureToken && !accessContext.SecureToken.IsValid)
            return false;

        foreach (var target in accessContext.Targets)
        {
            accessContext.ActorParticipations.TryGetValue(target.GameId, out var actorParticipation);
            accessContext.StaticTokenParticipations.TryGetValue(target.GameId, out var staticTokenParticipation);
            accessContext.SecureTokenParticipations.TryGetValue(target.GameId, out var secureTokenParticipation);

            if (IsTargetAuthorized(target, actorParticipation, staticTokenParticipation, secureTokenParticipation))
                return true;
        }

        return false;
    }

    private static bool IsTargetAuthorized(
        DownloadTarget target,
        Participation? actorParticipation,
        Participation? staticTokenParticipation,
        Participation? secureTokenParticipation)
    {
        if (target.SourceTeamId is null)
            return actorParticipation is not null
                || staticTokenParticipation is not null
                || secureTokenParticipation is not null;

        var sourceTeamId = target.SourceTeamId.Value;
        return actorParticipation?.TeamId == sourceTeamId
            || staticTokenParticipation?.TeamId == sourceTeamId
            || secureTokenParticipation?.TeamId == sourceTeamId;
    }

    private async Task LogDownloadAsync(
        DownloadAccessContext accessContext,
        ClaimsPrincipal? user,
        string? token,
        CancellationToken cancellationToken)
    {
        try
        {
            if (accessContext.Targets.Count == 0)
                return;

            var actorUserId = accessContext.ActorUserId;
            var secureToken = accessContext.SecureToken;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            foreach (var challengeGroup in accessContext.Targets.GroupBy(t => new { t.GameId, t.ChallengeId, t.ChallengeTitle }))
            {
                accessContext.ActorParticipations.TryGetValue(challengeGroup.Key.GameId, out var actorParticipation);

                accessContext.StaticTokenParticipations.TryGetValue(challengeGroup.Key.GameId, out var staticTokenParticipation);
                accessContext.SecureTokenParticipations.TryGetValue(challengeGroup.Key.GameId, out var secureTokenParticipation);
                var tokenParticipation = staticTokenParticipation ?? secureTokenParticipation;

                var selectedTarget = SelectRelevantTarget(challengeGroup, actorParticipation?.TeamId, tokenParticipation?.TeamId);
                if (!IsTargetAuthorized(selectedTarget, actorParticipation, staticTokenParticipation, secureTokenParticipation))
                    continue;

                var participation = actorParticipation ?? tokenParticipation;
                var teamId = participation?.TeamId ?? selectedTarget.SourceTeamId;
                var teamName = participation?.Team?.Name ?? selectedTarget.SourceTeamName ?? "Unknown";

                if (teamId is null)
                    continue;

                var eventUserId = actorUserId ?? secureToken.UserId;
                var downloadSource = BuildDownloadSource(user, actorUserId, token, secureToken);
                var actorSource = ResolveActorSource(actorUserId, token, secureToken);
                var tokenType = ResolveTokenType(token, secureToken);

                var sourceTeamId = tokenParticipation?.TeamId ?? selectedTarget.SourceTeamId;
                var sourceTeamName = tokenParticipation?.Team?.Name ?? selectedTarget.SourceTeamName ?? "Unknown";
                var abuseTag = string.Empty;
                var tokenAbuse = false;
                var hasToken = !string.IsNullOrWhiteSpace(token);
                var tokenSourceTeamId = hasToken ? sourceTeamId : null;
                var tokenSourceTeamName = hasToken ? sourceTeamName : null;
                var tokenSourceUserId = secureToken.IsValid ? secureToken.UserId : null;
                var tokenSourceUserName = secureToken.IsValid ? secureToken.UserName : null;

                if (hasToken &&
                    actorParticipation is not null &&
                    sourceTeamId is not null &&
                    sourceTeamId != actorParticipation.TeamId)
                {
                    tokenAbuse = true;
                    abuseTag = !string.IsNullOrWhiteSpace(secureToken.UserName)
                        ? $" [Token Source: {secureToken.UserName} (Team {sourceTeamName})]"
                        : $" [Token Source: Team {sourceTeamName}]";
                }

                var downloadMetadata = new DownloadEventLogMetadata
                {
                    ChallengeId = challengeGroup.Key.ChallengeId,
                    ChallengeTitle = challengeGroup.Key.ChallengeTitle,
                    ActorTeamId = teamId.Value,
                    ActorTeamName = teamName,
                    ActorUserId = eventUserId,
                    ActorUserName = actorUserId is not null
                        ? user?.Identity?.Name
                        : secureToken.UserName,
                    ActorSource = actorSource,
                    TokenType = tokenType,
                    TokenAbuse = tokenAbuse,
                    TokenSourceTeamId = tokenSourceTeamId,
                    TokenSourceTeamName = tokenSourceTeamName,
                    TokenSourceUserId = tokenSourceUserId,
                    TokenSourceUserName = tokenSourceUserName
                };

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
                        ipAddress,
                        downloadMetadata.Serialize()
                    ]
                }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to log download for target {TargetIdentifier}", accessContext.TargetIdentifier);
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

    private async Task<SecureTokenContext> ParseSecureToken(string? token, string expectedTarget, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || !TryUnprotectTokenPayload(token, out var payload))
            return new(null, null, false);

        var parts = payload.Split('|');
        if (parts.Length != 4 || parts[0] != "v1")
            return new(null, null, false);

        if (!string.Equals(parts[1], expectedTarget, StringComparison.OrdinalIgnoreCase))
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

    private async Task<List<DownloadTarget>> ResolveDownloadTargetsByHash(string hash, CancellationToken cancellationToken)
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

    private async Task<List<DownloadTarget>> ResolveDownloadTargetsByAttachmentId(int attachmentId, CancellationToken cancellationToken)
    {
        var staticTargets = await context.GameChallenges
            .AsNoTracking()
            .Where(c => c.AttachmentId == attachmentId && c.Attachment != null && c.Attachment.Type == FileType.Remote)
            .Select(c => new DownloadTarget(c.GameId, c.Id, c.Title, null, null))
            .ToListAsync(cancellationToken);

        var dynamicTargets = await context.GameInstances
            .AsNoTracking()
            .Where(i => i.FlagContext != null
                        && i.FlagContext.AttachmentId == attachmentId
                        && i.FlagContext.Attachment != null
                        && i.FlagContext.Attachment.Type == FileType.Remote)
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

    private static string ResolveActorSource(Guid? actorUserId, string? token, SecureTokenContext secureToken)
    {
        if (actorUserId is not null)
            return "authenticated_user";

        if (secureToken.IsValid && secureToken.UserId is not null)
            return "secure_token";

        if (!string.IsNullOrWhiteSpace(token))
            return "static_token";

        return "anonymous";
    }

    private static string ResolveTokenType(string? token, SecureTokenContext secureToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return "none";

        return secureToken.IsValid ? "secure" : "static";
    }
}
