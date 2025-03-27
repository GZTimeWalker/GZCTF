using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Channels;
using FluentStorage;
using FluentStorage.Blobs;
using GZCTF.Middlewares;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Admin;
using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace GZCTF.Controllers;

/// <summary>
/// Game related APIs
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class GameController(
    ILogger<GameController> logger,
    UserManager<UserInfo> userManager,
    ChannelWriter<Submission> channelWriter,
    IBlobStorage storage,
    IBlobRepository blobService,
    IGameRepository gameRepository,
    ITeamRepository teamRepository,
    IGameEventRepository eventRepository,
    IGameNoticeRepository noticeRepository,
    ICheatInfoRepository cheatInfoRepository,
    IContainerRepository containerRepository,
    IGameEventRepository gameEventRepository,
    ISubmissionRepository submissionRepository,
    IGameChallengeRepository challengeRepository,
    IGameInstanceRepository gameInstanceRepository,
    IParticipationRepository participationRepository,
    IOptionsSnapshot<ContainerPolicy> containerPolicy,
    IStringLocalizer<Program> localizer) : ControllerBase
{
    /// <summary>
    /// Get the recent games
    /// </summary>
    /// <remarks>
    /// Retrieves recent game in three weeks
    /// </remarks>
    /// <param name="limit">Limit of the number of games</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game information</response>
    [HttpGet("Recent")]
    [ResponseCache(Duration = 300)]
    [ProducesResponseType(typeof(BasicGameInfoModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> RecentGames(
        [FromQuery][Range(0, 50)] int limit,
        CancellationToken token)
    {
        var games = await gameRepository.GetRecentGames(token);

        return Ok(limit > 0 ? games.Take(limit).ToArray() : games);
    }

    /// <summary>
    /// Get games
    /// </summary>
    /// <remarks>
    /// Retrieves game information in specified range
    /// </remarks>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game notices</response>
    /// <response code="400">Game not found</response>
    [HttpGet]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Query))]
    [ResponseCache(VaryByQueryKeys = ["count", "skip"], Duration = 300)]
    [ProducesResponseType(typeof(ArrayResponse<BasicGameInfoModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Games([FromQuery][Range(0, 50)] int count = 10,
        [FromQuery] int skip = 0, CancellationToken token = default)
        => Ok(await gameRepository.GetGameInfo(count, skip, token));

    /// <summary>
    /// Get detailed game information
    /// </summary>
    /// <remarks>
    /// Retrieves detailed information about the game
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game information</response>
    /// <response code="404">Game not found</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DetailedGameInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Game(int id, CancellationToken token)
    {
        var context = await GetContextInfo(id, token: token);

        if (context.Game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var count = await participationRepository.GetParticipationCount(context.Game, token);

        return Ok(DetailedGameInfoModel.FromGame(context.Game, count)
            .WithParticipation(context.Participation));
    }

    /// <summary>
    /// Join a game
    /// </summary>
    /// <remarks>
    /// Join a game; requires User permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully joined the game</response>
    /// <response code="403">Unauthorized operation or invalid operation</response>
    /// <response code="404">Game not found</response>
    [RequireUser]
    [HttpPost("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> JoinGame(int id, [FromBody] GameJoinModel model, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        if (!game.PracticeMode && game.EndTimeUtc < DateTimeOffset.UtcNow)
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_Ended)], ErrorCodes.GameEnded));

        if (!string.IsNullOrEmpty(game.InviteCode) && game.InviteCode != model.InviteCode)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_InvalidInvitationCode)]));

        if (!game.IsValidDivision(model.Division))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_InvalidDivision)]));

        var user = await userManager.GetUserAsync(User);
        var team = await teamRepository.GetTeamById(model.TeamId, token);

        if (team is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Team_NotFound)],
                StatusCodes.Status404NotFound));

        if (team.Members.All(u => u.Id != user!.Id))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotMemberOfTeam)]));

        // If already joined (not rejected)
        if (await participationRepository.CheckRepeatParticipation(user!, game, token))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_InOtherTeam)]));

        // Remove all existing participations
        await participationRepository.RemoveUserParticipations(user!, game, token);

        // Try to get participation object
        var part = await participationRepository.GetParticipation(team, game, token);

        // If the team is not in the game, create a new participation object
        if (part is null)
        {
            // Create new participation object, do not update team-game-user triple tuple
            part = new()
            {
                Game = game,
                Team = team,
                Division = model.Division,
                Token = gameRepository.GetToken(game, team)
            };

            participationRepository.Add(part);
        }

        if (game.TeamMemberCountLimit > 0 && part.Members.Count >= game.TeamMemberCountLimit)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_TeamMemberLimitExceeded)]));

        // Add current user to the team
        part.Members.Add(new(user!, game, team));

        // Set division as the last request
        part.Division = model.Division;

        if (part.Status == ParticipationStatus.Rejected)
            part.Status = ParticipationStatus.Pending;

        await participationRepository.SaveAsync(token);

        if (game.AcceptWithoutReview)
            await participationRepository.UpdateParticipation(part,
                new ParticipationEditModel(ParticipationStatus.Accepted), token);

        logger.Log(StaticLocalizer[nameof(Resources.Program.Game_JoinSucceeded), team.Name, game.Title], user,
            TaskStatus.Success);

        return Ok();
    }

    /// <summary>
    /// Leave a game
    /// </summary>
    /// <remarks>
    /// Leave a game; requires User permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully left the game</response>
    /// <response code="403">Unauthorized operation or invalid operation</response>
    /// <response code="404">Game not found</response>
    [RequireUser]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LeaveGame(int id, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var user = await userManager.GetUserAsync(User);

        var part = await participationRepository.GetParticipation(user!, game, token);

        if (part is null || part.Members.All(u => u.UserId != user!.Id))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_CannotLeaveWithoutJoin)]));

        if (part.Status != ParticipationStatus.Pending && part.Status != ParticipationStatus.Rejected)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_CannotLeaveAfterApproval)]));

        // FIXME: After approval, new users can be added, but cannot exit?

        part.Members.RemoveWhere(u => u.UserId == user!.Id);

        if (part.Members.Count == 0)
            await participationRepository.RemoveParticipation(part, true, token);
        else
            await participationRepository.SaveAsync(token);

        return Ok();
    }

    /// <summary>
    /// Get the scoreboard
    /// </summary>
    /// <remarks>
    /// Retrieves the scoreboard data
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game information</response>
    /// <response code="400">Game not found</response>
    [HttpGet("{id:int}/Scoreboard")]
    [ProducesResponseType(typeof(ScoreboardModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Scoreboard([FromRoute] int id, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        if (DateTimeOffset.UtcNow < game.StartTimeUtc)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted)]));

        return Ok(await gameRepository.GetScoreboard(game, token));
    }

    /// <summary>
    /// Get game notices
    /// </summary>
    /// <remarks>
    /// Retrieves game notice data
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game notices</response>
    /// <response code="400">Game not found</response>
    [HttpGet("{id:int}/Notices")]
    [ProducesResponseType(typeof(GameNotice[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Notices([FromRoute] int id, [FromQuery][Range(0, 100)] int count = 100,
        [FromQuery] int skip = 0, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        if (DateTimeOffset.UtcNow < game.StartTimeUtc)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted)]));

        return Ok(await noticeRepository.GetNotices(game.Id, count, skip, token));
    }

    /// <summary>
    /// Get game events
    /// </summary>
    /// <remarks>
    /// Retrieves game event data; requires Monitor permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="count"></param>
    /// <param name="hideContainer">Hide container events</param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game events</response>
    /// <response code="400">Game not found</response>
    [RequireMonitor]
    [HttpGet("{id:int}/Events")]
    [ProducesResponseType(typeof(GameEvent[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Events([FromRoute] int id, [FromQuery] bool hideContainer = false,
        [FromQuery][Range(0, 100)] int count = 100, [FromQuery] int skip = 0, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        if (DateTimeOffset.UtcNow < game.StartTimeUtc)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted)]));

        return Ok(await eventRepository.GetEvents(game.Id, hideContainer, count, skip, token));
    }

    /// <summary>
    /// Get game submissions
    /// </summary>
    /// <remarks>
    /// Retrieves game submission data; requires Monitor permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="type">Submission type</param>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game submissions</response>
    /// <response code="400">Game not found</response>
    [RequireMonitor]
    [HttpGet("{id:int}/Submissions")]
    [ProducesResponseType(typeof(Submission[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submissions([FromRoute] int id, [FromQuery] AnswerResult? type = null,
        [FromQuery][Range(0, 100)] int count = 100, [FromQuery] int skip = 0, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        if (DateTimeOffset.UtcNow < game.StartTimeUtc)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted)]));

        return Ok(await submissionRepository.GetSubmissions(game, type, count, skip, token));
    }

    /// <summary>
    /// Get game cheat information
    /// </summary>
    /// <remarks>
    /// Retrieves game cheat data; requires Monitor permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game cheat data</response>
    /// <response code="400">Game not found</response>
    [RequireMonitor]
    [HttpGet("{id:int}/CheatInfo")]
    [ProducesResponseType(typeof(CheatInfoModel[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheatInfo([FromRoute] int id, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        if (DateTimeOffset.UtcNow < game.StartTimeUtc)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted)]));

        return Ok((await cheatInfoRepository.GetCheatInfoByGameId(game.Id, token))
            .Select(CheatInfoModel.FromCheatInfo));
    }

    /// <summary>
    /// Get challenges with traffic capturing enabled
    /// </summary>
    /// <remarks>
    /// Retrieves challenges with traffic capturing enabled; requires Monitor permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved challenge list</response>
    /// <response code="404">Capture information not found</response>
    [RequireMonitor]
    [HttpGet("Games/{id:int}/Captures")]
    [ProducesResponseType(typeof(ChallengeTrafficModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChallengesWithTrafficCapturing([FromRoute] int id, CancellationToken token)
    {
        var challenges = await challengeRepository.GetChallengesWithTrafficCapturing(id, token);

        var results = await Task.WhenAll(
            challenges.Select(c => ChallengeTrafficModel.FromChallengeAsync(c, storage, token))
        );

        return Ok(results);
    }

    /// <summary>
    /// Get team captures in a challenge
    /// </summary>
    /// <remarks>
    /// Retrieves the list of captured teams for a game challenge; requires Monitor permission
    /// </remarks>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved file list</response>
    /// <response code="404">Capture information not found</response>
    [RequireMonitor]
    [HttpGet("Captures/{challengeId:int}")]
    [ProducesResponseType(typeof(TeamTrafficModel[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChallengeTraffic([FromRoute] int challengeId, CancellationToken token)
    {
        var path = StoragePath.Combine(PathHelper.Capture, $"{challengeId}");

        var entries = await storage.ListAsync(path, cancellationToken: token);
        var participationIds = entries.Select(
                e => int.TryParse(e.Name, out var id) ? id : -1)
            .Where(id => id > 0).ToArray();

        if (participationIds.Length == 0)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_CaptureNotFound)],
                StatusCodes.Status404NotFound));

        var participation = await participationRepository.GetParticipationsByIds(participationIds, token);

        var results = await Task.WhenAll(
            participation.Select(p => TeamTrafficModel.FromParticipationAsync(p, challengeId, storage, token))
        );

        return Ok(results);
    }

    /// <summary>
    /// Get traffic files
    /// </summary>
    /// <remarks>
    /// Retrieves traffic packet files for a team and challenge; requires Monitor permission
    /// </remarks>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="partId">Team participation ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved file list</response>
    /// <response code="404">Capture information not found</response>
    [RequireMonitor]
    [HttpGet("Captures/{challengeId:int}/{partId:int}")]
    [ProducesResponseType(typeof(FileRecord[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTeamTraffic([FromRoute] int challengeId, [FromRoute] int partId,
        CancellationToken token)
    {
        var path = StoragePath.Combine(PathHelper.Capture, $"{challengeId}", $"{partId}");

        var blobs = await storage.ListAsync(path, cancellationToken: token);

        var results = blobs.Select(blob => new FileRecord
        {
            FileName = blob.Name,
            Size = blob.Size ?? 0,
            UpdateTime = blob.LastModificationTime ?? DateTimeOffset.MinValue
        }).ToArray();

        return Ok(results);
    }

    /// <summary>
    /// Download all traffic files
    /// </summary>
    /// <remarks>
    /// Downloads all traffic packet files for a team and challenge; requires Monitor permission
    /// </remarks>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="partId">Team participation ID</param>
    /// <param name="token">Token</param>
    /// <response code="200">Successfully retrieved files</response>
    /// <response code="404">Capture information not found</response>
    [RequireMonitor]
    [HttpGet("Captures/{challengeId:int}/{partId:int}/All")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAllTeamTraffic([FromRoute] int challengeId, [FromRoute] int partId,
        CancellationToken token)
    {
        var path = StoragePath.Combine(PathHelper.Capture, $"{challengeId}", $"{partId}");

        var filename = $"Capture-{challengeId}-{partId}-{DateTimeOffset.UtcNow:yyyyMMdd-HH.mm.ssZ}";

        return new TarDirectoryResult(storage, path, filename, token);
    }

    /// <summary>
    /// Deletes all traffic files
    /// </summary>
    /// <remarks>
    /// Deletes a team's traffic packet files for a challenge; requires Monitor permission
    /// </remarks>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="partId">Team participation ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully deleted files</response>
    /// <response code="404">Capture information not found</response>
    [RequireMonitor]
    [HttpDelete("Captures/{challengeId:int}/{partId:int}/All")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAllTeamTraffic([FromRoute] int challengeId, [FromRoute] int partId,
        CancellationToken token)
    {
        try
        {
            var path = StoragePath.Combine(PathHelper.Capture, $"{challengeId}", $"{partId}");

            await storage.DeleteAsync(path, token);

            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(new RequestResponse(e.Message));
        }
    }

    /// <summary>
    /// Get a traffic file
    /// </summary>
    /// <remarks>
    /// Retrieves a traffic packet file; requires Monitor permission
    /// </remarks>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="partId">Team participation ID</param>
    /// <param name="filename">Traffic packet filename</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved file</response>
    /// <response code="404">Capture information not found</response>
    [RequireMonitor]
    [HttpGet("Captures/{challengeId:int}/{partId:int}/{filename}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeamTraffic([FromRoute] int challengeId, [FromRoute] int partId,
        [FromRoute] string filename, CancellationToken token)
    {
        try
        {
            var path = StoragePath.Combine(PathHelper.Capture, $"{challengeId}", $"{partId}", filename);

            if (!await storage.ExistsAsync(path, token))
                return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_CaptureNotFound)]));

            var stream = await storage.OpenReadAsync(path, token);

            return File(stream, MediaTypeNames.Application.Octet, filename);
        }
        catch
        {
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_CaptureNotFound)]));
        }
    }

    /// <summary>
    /// Deletes a traffic file
    /// </summary>
    /// <remarks>
    /// Deletes a traffic packet file; requires Monitor permission
    /// </remarks>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="partId">Team participation ID</param>
    /// <param name="filename">Traffic packet filename</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully deleted file</response>
    /// <response code="404">Capture information not found</response>
    [RequireMonitor]
    [HttpDelete("Captures/{challengeId:int}/{partId:int}/{filename}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTeamTraffic([FromRoute] int challengeId, [FromRoute] int partId,
        [FromRoute] string filename, CancellationToken token)
    {
        try
        {
            var path = StoragePath.Combine(PathHelper.Capture, $"{challengeId}", $"{partId}", filename);

            if (!await storage.ExistsAsync(path, token))
                return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_CaptureNotFound)]));

            await storage.DeleteAsync(path, token);

            return Ok();
        }
        catch
        {
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_CaptureNotFound)]));
        }
    }

    /// <summary>
    /// Get team details in a game
    /// </summary>
    /// <remarks>
    /// Retrieves all challenges of the game; requires User permission and active team participation
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game challenge information</response>
    /// <response code="400">Invalid operation</response>
    /// <response code="404">Game not found</response>
    [RequireUser]
    [HttpGet("{id:int}/Details")]
    [ProducesResponseType(typeof(GameDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChallengesWithTeamInfo([FromRoute] int id, CancellationToken token)
    {
        var context = await GetContextInfo(id, token: token);

        if (context.Result is not null)
            return context.Result;

        var scoreboard = await gameRepository.GetScoreboard(context.Game!, token);

        var boardItem = scoreboard.Items.TryGetValue(context.Participation!.TeamId, out var item)
            ? item
            : new()
            {
                Avatar = context.Participation!.Team.AvatarUrl,
                Rank = 0,
                Name = context.Participation!.Team.Name,
                Id = context.Participation!.TeamId
            };

        return Ok(new GameDetailModel
        {
            ScoreboardItem = boardItem,
            TeamToken = context.Participation!.Token,
            Challenges = scoreboard.Challenges,
            ChallengeCount = scoreboard.ChallengeCount,
            WriteupRequired = context.Game!.WriteupRequired,
            WriteupDeadline = context.Game!.WriteupDeadline
        });
    }

    /// <summary>
    /// Get all game participations
    /// </summary>
    /// <remarks>
    /// Retrieves all participation information of the game; requires Admin permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game participation information</response>
    /// <response code="400">Invalid operation</response>
    /// <response code="404">Game not found</response>
    [RequireAdmin]
    [HttpGet("{id:int}/Participations")]
    [ProducesResponseType(typeof(ParticipationInfoModel[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Participations([FromRoute] int id, CancellationToken token = default)
    {
        var context = await GetContextInfo(id, token: token);

        if (context.Game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)]));

        return Ok((await participationRepository.GetParticipations(context.Game!, token))
            .Select(ParticipationInfoModel.FromParticipation));
    }

    /// <summary>
    /// Downloads the scoreboard
    /// </summary>
    /// <remarks>
    /// Downloads the game scoreboard; requires Monitor permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="excelHelper"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully downloaded game scoreboard</response>
    /// <response code="400">Invalid operation</response>
    /// <response code="404">Game not found</response>
    [RequireMonitor]
    [HttpGet("{id:int}/ScoreboardSheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public async Task<IActionResult> ScoreboardSheet([FromRoute] int id, [FromServices] ExcelHelper excelHelper,
        CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)]));

        if (DateTimeOffset.UtcNow < game.StartTimeUtc)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted)]));

        try
        {
            var scoreboard = await gameRepository.GetScoreboardWithMembers(game, token);
            var stream = excelHelper.GetScoreboardExcel(scoreboard, game);
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{game.Title}-Scoreboard-{DateTimeOffset.Now:yyyyMMdd-HH.mm.ssZ}.xlsx");
        }
        catch (Exception ex)
        {
            logger.SystemLog(StaticLocalizer[nameof(Resources.Program.Game_ScoreboardDownloadFailed)],
                TaskStatus.Failed, LogLevel.Error);
            logger.LogErrorMessage(ex, ex.Message);
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_ScoreboardDownloadFailed)]));
        }
    }

    /// <summary>
    /// Downloads all submissions
    /// </summary>
    /// <remarks>
    /// Downloads all submissions of the game; requires Monitor permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="excelHelper"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully downloaded all game submissions</response>
    /// <response code="400">Invalid operation</response>
    /// <response code="404">Game not found</response>
    [RequireMonitor]
    [HttpGet("{id:int}/SubmissionSheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public async Task<IActionResult> SubmissionSheet([FromRoute] int id, [FromServices] ExcelHelper excelHelper,
        CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)]));

        if (DateTimeOffset.UtcNow < game.StartTimeUtc)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted)]));

        var submissions = await submissionRepository.GetSubmissions(game, count: 0, token: token);

        var stream = excelHelper.GetSubmissionExcel(submissions);
        stream.Seek(0, SeekOrigin.Begin);

        return File(stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{game.Title}_Submissions_{DateTimeOffset.Now:yyyyMMddHHmmss}.xlsx");
    }

    /// <summary>
    /// Get challenge information
    /// </summary>
    /// <remarks>
    /// Retrieves challenge information; requires User permission and active team participation
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game challenge information</response>
    /// <response code="400">Invalid operation</response>
    /// <response code="404">Game not found</response>
    [RequireUser]
    [HttpGet("{id:int}/Challenges/{challengeId:int}")]
    [ProducesResponseType(typeof(ChallengeDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChallenge([FromRoute] int id, [FromRoute] int challengeId,
        CancellationToken token)
    {
        if (id <= 0 || challengeId <= 0)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        var context = await GetContextInfo(id, token: token);

        if (context.Result is not null)
            return context.Result;

        var instance = await gameInstanceRepository.GetInstance(context.Participation!, challengeId, token);

        if (instance is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_ChallengeNotFound)],
                StatusCodes.Status404NotFound));

        return Ok(ChallengeDetailModel.FromInstance(instance));
    }

    /// <summary>
    /// Submits a flag
    /// </summary>
    /// <remarks>
    /// Submits a flag; requires User permission and active team participation
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="model">Flag submission</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game challenge information</response>
    /// <response code="400">Invalid operation</response>
    /// <response code="404">Game not found</response>
    [RequireUser]
    [HttpPost("{id:int}/Challenges/{challengeId:int}")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Submit))]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submit([FromRoute] int id, [FromRoute] int challengeId,
        [FromBody] FlagSubmitModel model, CancellationToken token)
    {
        var context = await GetContextInfo(id, challengeId, token: token);

        if (context.Result is not null)
            return context.Result;

        Submission submission = new()
        {
            Answer = model.Flag.Trim(),
            Game = context.Game!,
            User = context.User!,
            GameChallenge = context.Challenge!,
            Team = context.Participation!.Team,
            Participation = context.Participation!,
            Status = AnswerResult.FlagSubmitted,
            SubmitTimeUtc = DateTimeOffset.UtcNow
        };

        submission = await submissionRepository.AddSubmission(submission, token);

        // send to flag checker service
        await channelWriter.WriteAsync(submission, token);

        return Ok(submission.Id);
    }

    /// <summary>
    /// Queries flag status
    /// </summary>
    /// <remarks>
    /// Queries flag status; requires User permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="submitId">Submission ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved submission status</response>
    /// <response code="404">Submission not found</response>
    [RequireUser]
    [HttpGet("{id:int}/Challenges/{challengeId:int}/Status/{submitId:int}")]
    [ProducesResponseType(typeof(AnswerResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Status([FromRoute] int id, [FromRoute] int challengeId, [FromRoute] int submitId,
        CancellationToken token)
    {
        var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (claimId is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_SubmissionNotFound)],
                StatusCodes.Status404NotFound));

        var submission =
            await submissionRepository.GetSubmission(id, challengeId, Guid.Parse(claimId), submitId, token);

        if (submission is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Submission_NotFound)],
                StatusCodes.Status404NotFound));

        return Ok(submission.Status switch
        {
            AnswerResult.CheatDetected => AnswerResult.WrongAnswer,
            var x => x
        });
    }

    /// <summary>
    /// Get writeup information
    /// </summary>
    /// <remarks>
    /// Retrieves post-game writeup submission information; requires User permission
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully submitted writeup</response>
    /// <response code="400">Submission does not meet requirements</response>
    /// <response code="404">Game not found</response>
    [RequireUser]
    [HttpGet("{id:int}/Writeup")]
    [ProducesResponseType(typeof(BasicWriteupInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetWriteup([FromRoute] int id, CancellationToken token)
    {
        var context = await GetContextInfo(id, denyAfterEnded: false, token: token);

        if (context.Result is not null)
            return context.Result;

        return Ok(BasicWriteupInfoModel.FromParticipation(context.Participation!));
    }

    /// <summary>
    /// Submits a writeup
    /// </summary>
    /// <remarks>
    /// Submits a post-game writeup; requires User permission
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="file">File</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully submitted writeup</response>
    /// <response code="400">Submission does not meet requirements</response>
    /// <response code="404">Game not found</response>
    [RequireUser]
    [HttpPost("{id:int}/Writeup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitWriteup([FromRoute] int id, IFormFile file, CancellationToken token)
    {
        switch (file.Length)
        {
            case 0:
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_SizeZero)]));
            case > 20 * 1024 * 1024:
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_SizeTooLarge)]));
        }

        if (file.ContentType != "application/pdf" || Path.GetExtension(file.FileName) != ".pdf")
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_PdfOnly)]));

        var context = await GetContextInfo(id, denyAfterEnded: false, token: token);

        if (context.Result is not null)
            return context.Result;

        var game = context.Game!;

        if (!game.WriteupRequired)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_WriteupNotNeeded)]));

        var part = context.Participation!;
        var team = part.Team;

        if (DateTimeOffset.UtcNow > game.WriteupDeadline)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_DeadlineExpired)]));

        var wp = context.Participation!.Writeup;

        if (wp is not null)
            await blobService.DeleteBlob(wp, token);

        part.Writeup = await blobService.CreateOrUpdateBlob(file,
            $"Writeup-{game.Id}-{team.Id}-{DateTimeOffset.Now:yyyyMMdd-HH.mm.ssZ}.pdf", token);

        await participationRepository.SaveAsync(token);

        logger.Log(StaticLocalizer[nameof(Resources.Program.Game_WriteupSubmitted), team.Name, game.Title],
            context.User!,
            TaskStatus.Success);

        return Ok();
    }

    /// <summary>
    /// Creates a container
    /// </summary>
    /// <remarks>
    /// Creates a container; requires User permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game challenge information</response>
    /// <response code="404">Challenge not found</response>
    /// <response code="400">Container creation not allowed for challenge</response>
    [RequireUser]
    [HttpPost("{id:int}/Container/{challengeId:int}")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Container))]
    [ProducesResponseType(typeof(ContainerInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreateContainer([FromRoute] int id, [FromRoute] int challengeId,
        CancellationToken token)
    {
        var context = await GetContextInfo(id, token: token);

        if (context.Result is not null)
            return context.Result;

        var instance = await gameInstanceRepository.GetInstance(context.Participation!, challengeId, token);

        if (instance is null || !instance.Challenge.IsEnabled)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        if (!instance.Challenge.Type.IsContainer())
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerCreationNotAllowed)]));

        if (instance.IsContainerOperationTooFrequent)
            return new JsonResult(new RequestResponse(localizer[nameof(Resources.Program.Game_OperationTooFrequent)],
                StatusCodes.Status429TooManyRequests))
            { StatusCode = StatusCodes.Status429TooManyRequests };

        if (instance.Container is not null)
        {
            if (instance.Container.Status == ContainerStatus.Running)
                return BadRequest(
                    new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerAlreadyCreated)]));

            await containerRepository.DestroyContainer(instance.Container, token);
        }

        return await gameInstanceRepository.CreateContainer(instance, context.Participation!.Team, context.User!,
                context.Game!, token) switch
        {
            null or (TaskStatus.Failed, null) => BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerCreationFailed)])),
            (TaskStatus.Denied, null) => BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerNumberLimitExceeded),
                    context.Game!.ContainerCountLimit])),
            (TaskStatus.Success, var x) => Ok(ContainerInfoModel.FromContainer(x!)),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Extends container lifetime
    /// </summary>
    /// <remarks>
    /// Extends container lifetime; requires User permission and can only be extended two hours within ten minutes before expiration
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game challenge container information</response>
    /// <response code="404">Challenge not found</response>
    /// <response code="400">Container not created or cannot be extended</response>
    [RequireUser]
    [HttpPost("{id:int}/Container/{challengeId:int}/Extend")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Container))]
    [ProducesResponseType(typeof(ContainerInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExtendContainerLifetime([FromRoute] int id, [FromRoute] int challengeId,
        CancellationToken token)
    {
        var context = await GetContextInfo(id, token: token);

        if (context.Result is not null)
            return context.Result;

        var instance = await gameInstanceRepository.GetInstance(context.Participation!, challengeId, token);

        if (instance is null || !instance.Challenge.IsEnabled)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        if (!instance.Challenge.Type.IsContainer())
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerCreationNotAllowed)]));

        if (instance.Container is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerNotCreated)]));

        if (instance.Container.ExpectStopAt - DateTimeOffset.UtcNow >
            TimeSpan.FromMinutes(containerPolicy.Value.RenewalWindow))
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerExtensionNotAvailable)]));

        await containerRepository.ExtendLifetime(instance.Container,
            TimeSpan.FromMinutes(containerPolicy.Value.ExtensionDuration), token);

        return Ok(ContainerInfoModel.FromContainer(instance.Container));
    }

    /// <summary>
    /// Deletes a container
    /// </summary>
    /// <remarks>
    /// Deletes a container; requires User permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully deleted container</response>
    /// <response code="404">Challenge not found</response>
    /// <response code="400">Container creation not allowed for challenge</response>
    [RequireUser]
    [HttpDelete("{id:int}/Container/{challengeId:int}")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Container))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DeleteContainer([FromRoute] int id, [FromRoute] int challengeId,
        CancellationToken token)
    {
        var context = await GetContextInfo(id, token: token);

        if (context.Result is not null)
            return context.Result;

        var instance = await gameInstanceRepository.GetInstance(context.Participation!, challengeId, token);

        if (instance is null || !instance.Challenge.IsEnabled)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        if (!instance.Challenge.Type.IsContainer())
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerCreationNotAllowed)]));

        if (instance.Container is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerNotCreated)]));

        if (instance.IsContainerOperationTooFrequent)
            return new JsonResult(new RequestResponse(localizer[nameof(Resources.Program.Game_OperationTooFrequent)],
                StatusCodes.Status429TooManyRequests))
            { StatusCode = StatusCodes.Status429TooManyRequests };

        var destroyId = instance.Container.ContainerId;

        if (!await containerRepository.DestroyContainer(instance.Container, token))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerDeletionFailed)]));

        instance.LastContainerOperation = DateTimeOffset.UtcNow;

        await gameEventRepository.AddEvent(
            new()
            {
                Type = EventType.ContainerDestroy,
                GameId = context.Game!.Id,
                TeamId = context.Participation!.TeamId,
                UserId = context.User!.Id,
                Values = [instance.Challenge.Id.ToString(), instance.Challenge.Title]
            }, token);

        logger.Log(
            StaticLocalizer[nameof(Resources.Program.Game_ContainerDeleted), context.Participation!.Team.Name,
                instance.Challenge.Title,
                destroyId],
            context.User, TaskStatus.Success);

        return Ok();
    }

    async Task<ContextInfo> GetContextInfo(int id, int challengeId = 0, bool withFlag = false,
        bool denyAfterEnded = true, CancellationToken token = default)
    {
        ContextInfo res = new()
        {
            User = await userManager.GetUserAsync(User),
            Game = await gameRepository.GetGameById(id, token)
        };

        if (res.Game is null)
            return res.WithResult(NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound)));

        var part = await participationRepository.GetParticipation(res.User!, res.Game, token);

        if (part is null)
            return res.WithResult(
                BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotParticipated)])));

        res.Participation = part;

        if (part.Status != ParticipationStatus.Accepted)
            return res.WithResult(
                BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_ParticipationNotAccepted)])));

        if (DateTimeOffset.UtcNow < res.Game.StartTimeUtc)
            return res.WithResult(
                BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted),
                    ErrorCodes.GameNotStarted])));

        if (denyAfterEnded && !res.Game.PracticeMode && res.Game.EndTimeUtc < DateTimeOffset.UtcNow)
            return res.WithResult(
                BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_Ended)], ErrorCodes.GameEnded)));

        if (challengeId <= 0)
            return res;

        var challenge = await challengeRepository.GetChallenge(id, challengeId, token);

        if (challenge is null)
            return res.WithResult(NotFound(new RequestResponse(
                localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound)));

        if (withFlag)
            await challengeRepository.LoadFlags(challenge, token);

        res.Challenge = challenge;

        return res;
    }

    class ContextInfo
    {
        public GameChallenge? Challenge;
        public Game? Game;
        public Participation? Participation;
        public IActionResult? Result;
        public UserInfo? User;

        public ContextInfo WithResult(IActionResult res)
        {
            Result = res;
            return this;
        }
    }
}
