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
    [ResponseCache(Duration = 60 * 60 * 24 * 7)]
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
    [ResponseCache(Duration = 60 * 60 * 24 * 7)]
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

    private async Task LogDownloadAsync(string hash, ClaimsPrincipal? user, string? token, CancellationToken cancellationToken)
    {
        // Identify user ID if authenticated
        Guid? userId = null;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdString, out var parsedId))
            {
                userId = parsedId;
            }
        }

        string? secureTokenHash = null;

        // Try to parse secure token if provided
        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                // Try Unprotect
                string payload;
                if (token.Length > 100 && !token.Contains('|')) // Assume Base64Url Encoded path token
                {
                    try
                    {
                        var cipher = WebEncoders.Base64UrlDecode(token);
                        payload = Encoding.UTF8.GetString(_protector.Unprotect(cipher));
                    }
                    catch
                    {
                        // Fallback to old format or team token attempt
                        payload = _protector.Unprotect(token);
                    }
                }
                else
                {
                    payload = _protector.Unprotect(token);
                }
                
                var parts = payload.Split('|');
                if (parts.Length == 4 && parts[0] == "v1")
                {
                    var tokenHash = parts[1];
                    var tokenUserIdString = parts[2];
                    var tokenExpiryTicks = long.Parse(parts[3]);

                    // Verify Expired
                    if (DateTimeOffset.UtcNow.Ticks <= tokenExpiryTicks && tokenHash == hash)
                    {
                        if (Guid.TryParse(tokenUserIdString, out var parsedTokenUserId))
                        {
                            userId = parsedTokenUserId; // Use the user ID from the token
                            secureTokenHash = tokenHash; // Mark as secure token used
                        }
                    }
                }
            }
            catch
            {
                // Not a valid secure token, fall back to check as static team token
            }
        }

        // If neither authenticated nor has valid token (secure or team), we can't track
        if (userId == null && string.IsNullOrEmpty(token))
            return;

        try
        {
            // Find games where this file is an attachment for a challenge
            var challenges = await context.GameChallenges
                .Include(c => c.Attachment)
                .ThenInclude(a => a!.LocalFile)
                .Where(c => c.Attachment != null && c.Attachment.LocalFile!.Hash == hash)
                .Select(c => new { c.Id, c.Title, c.GameId })
                .ToArrayAsync(cancellationToken);

            if (challenges.Length == 0)
                return;

            foreach (var challenge in challenges)
            {
                Participation? participation = null;

                if (userId != null)
                {
                    // Check by User ID
                    participation = await context.Participations
                        .Include(p => p.Team)
                        .FirstOrDefaultAsync(p => p.GameId == challenge.GameId && p.Members.Any(m => m.UserId == userId), cancellationToken);
                }
                
                // Check token owner if token is present
                Participation? tokenParticipation = null;
                if (!string.IsNullOrEmpty(token))
                {
                     tokenParticipation = await context.Participations
                        .Include(p => p.Team)
                        .FirstOrDefaultAsync(p => p.GameId == challenge.GameId && p.Token == token, cancellationToken);
                }

                // If not logged in, fallback to token participation
                if (participation == null && tokenParticipation != null)
                {
                    participation = tokenParticipation;
                }

                if (participation == null)
                    continue;

                string downloadSource = "Unknown";
                string abuseTag = "";
                
                if (user?.Identity?.IsAuthenticated == true)
                {
                        downloadSource = $"User {user.Identity.Name}";
                        
                        // Detect Token Abuse: Authenticated user using another team's token
                        if (tokenParticipation != null && tokenParticipation.Id != participation.Id)
                        {
                            abuseTag = $" [Token Abuse: {tokenParticipation.Team?.Name ?? "Unknown"}]";
                        }
                }
                else if (secureTokenHash != null && userId != null) // It was a secure token
                {
                        var userInfo = await context.Users.FindAsync(new object[] { userId }, cancellationToken);
                        downloadSource = $"User {userInfo?.UserName ?? "Unknown"} (via Secure Token)";
                }
                else 
                        downloadSource = "Team Member (via Static Token)";

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                var evt = new GameEvent
                {
                    GameId = challenge.GameId,
                    TeamId = participation.TeamId,
                    UserId = userId, // Can be null if using static token
                    Type = EventType.Download,
                    PublishTimeUtc = DateTimeOffset.UtcNow,
                    Values = new List<string>
                    {
                        challenge.Id.ToString(),
                        "Attachment Download",
                        $"{downloadSource} from team {participation.Team?.Name ?? "Unknown"} downloaded attachment for challenge {challenge.Title}.{abuseTag}",
                        ipAddress
                    }
                };

                await eventRepository.AddEvent(evt, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to log download for hash {Hash}", hash);
        }
    }
}
