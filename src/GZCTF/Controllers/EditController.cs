using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using GZCTF.Extensions;
using GZCTF.Middlewares;
using GZCTF.Models.Request.Edit;
using GZCTF.Models.Request.Game;
using GZCTF.Models.Request.Info;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using GZCTF.Services.Container.Manager;
using GZCTF.Services.Transfer;
using GZCTF.Storage.Interface;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using NSwag.Annotations;
using GZCTF.Models.Request.Admin;
using GZCTF.Models.Response.Admin;

namespace GZCTF.Controllers;

/// <summary>
/// Data Modification APIs
/// </summary>

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class EditController(
    CacheHelper cacheHelper,
    UserManager<UserInfo> userManager,
    ILogger<EditController> logger,
    IPostRepository postRepository,
    IContainerRepository containerRepository,
    IGameChallengeRepository challengeRepository,
    IGameInstanceRepository instanceRepository,
    IGameNoticeRepository gameNoticeRepository,
    IGameRepository gameRepository,
    IContainerManager containerService,
    IBlobRepository blobService,
    GameExportService exportService,
    GameImportService importService,
    ChallengeImportService challengeImportService,
    AppDbContext dbContext,
    IDataProtectionProvider dataProtectionProvider,
    IDivisionRepository divisionRepository,
    IStringLocalizer<Program> localizer) : Controller
{
    private readonly IDataProtector _repoWatchProtector =
        dataProtectionProvider.CreateProtector(RepoWatchProtection.Purpose);

    /// <summary>
    /// Add Post
    /// </summary>
    /// <remarks>
    /// Adding a post requires administrator privileges
    /// </remarks>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully added post</response>
    [RequireAdmin]
    [HttpPost("Posts")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddPost([FromBody] PostEditModel model, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        var res = await postRepository.CreatePost(new Post().Update(model, user!), token);
        return Ok(res.Id);
    }

    /// <summary>
    /// Update Post
    /// </summary>
    /// <remarks>
    /// Updating a post requires administrator privileges
    /// </remarks>
    /// <param name="id">Post ID</param>
    /// <param name="token"></param>
    /// <param name="model"></param>
    /// <response code="200">Successfully updated post</response>
    /// <response code="404">Post not found</response>
    [RequireAdmin]
    [HttpPut("Posts/{id}")]
    [ProducesResponseType(typeof(PostDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePost(string id, [FromBody] PostEditModel model, CancellationToken token)
    {
        var post = await postRepository.GetPostById(id, token);

        if (post is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Post_NotFound)],
                StatusCodes.Status404NotFound));

        var user = await userManager.GetUserAsync(User);

        await postRepository.UpdatePost(post.Update(model, user!), token);

        return Ok(PostDetailModel.FromPost(post));
    }

    /// <summary>
    /// Delete Post
    /// </summary>
    /// <remarks>
    /// Deleting a post requires administrator privileges
    /// </remarks>
    /// <param name="id">Post ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully deleted post</response>
    /// <response code="404">Post not found</response>
    [RequireAdmin]
    [HttpDelete("Posts/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePost(string id, CancellationToken token)
    {
        var post = await postRepository.GetPostById(id, token);

        if (post is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Post_NotFound)],
                StatusCodes.Status404NotFound));

        await postRepository.RemovePost(post, token);

        return Ok();
    }

    /// <summary>
    /// Add Game
    /// </summary>
    /// <remarks>
    /// Adding a game requires administrator privileges
    /// </remarks>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully added game</response>
    [RequireAdmin]
    [HttpPost("Games")]
    [ProducesResponseType(typeof(GameInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddGame([FromBody] GameInfoModel model, CancellationToken token)
    {
        var game = await gameRepository.CreateGame(new Game().Update(model), token);

        if (game is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_CreationFailed)]));

        await cacheHelper.FlushRecentGamesCache(token);

        return Ok(GameInfoModel.FromGame(game));
    }

    /// <summary>
    /// Get Game List
    /// </summary>
    /// <remarks>
    /// Retrieving the game list requires administrator privileges or event admin privileges
    /// </remarks>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="dbContext"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game list</response>
    [RequireUser]
    [HttpGet("Games")]
    [ProducesResponseType(typeof(ArrayResponse<GameInfoModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGames([FromQuery][Range(0, 100)] int count = 20, [FromQuery] int skip = 0,
        [FromServices] AppDbContext dbContext = null!, CancellationToken token = default)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized(new RequestResponse(localizer[nameof(Resources.Program.Auth_LoginRequired)],
                StatusCodes.Status401Unauthorized));

        if (user.Role == Role.Admin)
        {
            var games = await gameRepository.GetGames(count, skip, token);
            var total = await gameRepository.CountAsync(token);
            return Ok(games.Select(GameInfoModel.FromGame).ToResponse(total));
        }
        else
        {
            var managerGameIds = await dbContext.EventManagers
                .Where(e => e.UserId == user.Id)
                .Select(e => e.GameId)
                .ToArrayAsync(token);
            
            if (managerGameIds.Length == 0)
                 return Forbid();

            var games = await dbContext.Games
                .Where(g => managerGameIds.Contains(g.Id))
                .OrderByDescending(g => g.StartTimeUtc)
                .Skip(skip)
                .Take(count)
                .ToArrayAsync(token);
            
            var total = managerGameIds.Length;

            return Ok(games.Select(GameInfoModel.FromGame).ToResponse(total));
        }
    }

    /// <summary>
    /// Get Game
    /// </summary>
    /// <remarks>
    /// Retrieving a game requires administrator privileges
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game</response>
    [RequireGameAdmin]
    [HttpGet("Games/{id:int}")]
    [ProducesResponseType(typeof(GameInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGame([FromRoute] int id, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        return Ok(GameInfoModel.FromGame(game));
    }

    /// <summary>
    /// Get Game Hash Salt
    /// </summary>
    /// <remarks>
    /// Retrieving the game hash salt requires administrator privileges
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game hash salt</response>
    [OpenApiIgnore]
    [RequireGameAdmin]
    [HttpGet("Games/{id:int}/HashSalt")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHashSalt([FromRoute] int id, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        return Ok(game.TeamHashSalt);
    }

    /// <summary>
    /// Update Game
    /// </summary>
    /// <remarks>
    /// Updating a game requires administrator privileges
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully updated game</response>
    [RequireGameAdmin]
    [HttpPut("Games/{id:int}")]
    [ProducesResponseType(typeof(GameInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGame([FromRoute] int id, [FromBody] GameInfoModel model,
        CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        game.Update(model);
        await gameRepository.UpdateGame(game, token);

        return Ok(GameInfoModel.FromGame(game));
    }

    /// <summary>
    /// Delete Game
    /// </summary>
    /// <remarks>
    /// Deleting a game requires administrator privileges
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully deleted game</response>
    [RequireAdmin]
    [HttpDelete("Games/{id:int}")]
    [ProducesResponseType(typeof(GameInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGame([FromRoute] int id, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        return await gameRepository.DeleteGame(game, token) switch
        {
            TaskStatus.Success => Ok(),
            _ => BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_DeletionFailed)]))
        };
    }

    /// <summary>
    /// Clone a game as a template
    /// </summary>
    /// <remarks>
    /// Creates a new hidden game copied from the source game's settings and challenges.
    /// Flags for static challenges are copied; dynamic flag templates are preserved.
    /// Attachments are not duplicated — re-upload them in the new game.
    /// </remarks>
    /// <response code="200">New game ID</response>
    /// <response code="404">Source game not found</response>
    [RequireAdmin]
    [HttpPost("Games/{id:int}/Clone")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloneGame([FromRoute] int id, [FromBody] GameCloneModel model,
        [FromServices] AppDbContext dbContext, CancellationToken token = default)
    {
        var source = await gameRepository.GetGameById(id, token);
        if (source is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var sourceChallenges = model.IncludeChallenges
            ? await dbContext.GameChallenges.AsNoTracking()
                .Where(c => c.GameId == id)
                .Include(c => c.Flags)
                .ToListAsync(token)
            : [];

        var newGame = new Game
        {
            Title = model.Title.Trim(),
            Summary = source.Summary,
            Content = source.Content,
            PracticeMode = source.PracticeMode,
            AcceptWithoutReview = source.AcceptWithoutReview,
            WriteupRequired = source.WriteupRequired,
            WriteupNote = source.WriteupNote,
            TeamMemberCountLimit = source.TeamMemberCountLimit,
            ContainerCountLimit = source.ContainerCountLimit,
            BloodBonusValue = source.BloodBonusValue,
            StartTimeUtc = model.StartTimeUtc.ToUniversalTime(),
            EndTimeUtc = model.EndTimeUtc.ToUniversalTime(),
            Hidden = true,
        };
        await gameRepository.CreateGame(newGame, token);

        foreach (var src in sourceChallenges)
        {
            var clone = new GameChallenge
            {
                GameId = newGame.Id,
                Title = src.Title,
                Content = src.Content,
                Category = src.Category,
                Type = src.Type,
                Hints = src.Hints is null ? null : [.. src.Hints],
                FlagTemplate = src.FlagTemplate,
                FileName = src.FileName,
                ContainerImage = src.ContainerImage,
                MemoryLimit = src.MemoryLimit,
                StorageLimit = src.StorageLimit,
                CPUCount = src.CPUCount,
                ExposePort = src.ExposePort,
                NetworkMode = src.NetworkMode,
                EnableTrafficCapture = src.EnableTrafficCapture,
                DisableBloodBonus = src.DisableBloodBonus,
                OriginalScore = src.OriginalScore,
                MinScoreRate = src.MinScoreRate,
                Difficulty = src.Difficulty,
                SubmissionLimit = src.SubmissionLimit,
                IsEnabled = false,
            };
            dbContext.GameChallenges.Add(clone);
            await dbContext.SaveChangesAsync(token);

            foreach (var flag in src.Flags ?? [])
                dbContext.FlagContexts.Add(new FlagContext { Flag = flag.Flag, ChallengeId = clone.Id });

            if (src.Flags?.Count > 0)
                await dbContext.SaveChangesAsync(token);
        }

        logger.SystemLog($"Cloned game \"{source.Title}\" → \"{newGame.Title}\" (id={newGame.Id})",
            TaskStatus.Success, LogLevel.Information);

        return Ok(newGame.Id);
    }

    /// <summary>
    /// Delete All WriteUps
    /// </summary>
    /// <remarks>
    /// Deleting all WriteUps for a game requires administrator privileges
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully deleted game WriteUps</response>
    [RequireAdmin]
    [HttpDelete("Games/{id:int}/WriteUps")]
    [ProducesResponseType(typeof(GameInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGameWriteUps([FromRoute] int id, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        await gameRepository.DeleteAllWriteUps(game, token);

        return Ok();
    }

    /// <summary>
    /// Update Game Poster
    /// </summary>
    /// <remarks>
    /// Use this endpoint to update the game poster; administrator privileges required
    /// </remarks>
    /// <response code="200">Game poster URL</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Unauthorized user</response>
    [RequireGameAdmin]
    [HttpPut("Games/{id:int}/Poster")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGamePoster([FromRoute] int id, IFormFile file, CancellationToken token)
    {
        switch (file.Length)
        {
            case 0:
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_SizeZero)]));
            case > 3 * 1024 * 1024:
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_SizeTooLarge)]));
        }

        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var poster = await blobService.CreateOrUpdateImage(file, "poster", 0, token);

        if (poster is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_CreationFailed)]));

        game.PosterHash = poster.Hash;
        await gameRepository.UpdateGame(game, token);

        return Ok(poster.Url());
    }

    /// <summary>
    /// Add Game Notice
    /// </summary>
    /// <remarks>
    /// Adding a game notice requires administrator privileges
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="model">Notice content</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully added game notice</response>
    [RequireGameAdmin]
    [HttpPost("Games/{id:int}/Notices")]
    [ProducesResponseType(typeof(GameNotice), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddGameNotice([FromRoute] int id, [FromBody] GameNoticeModel model,
        CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var publishTime = model.PublishAt is { } at && at > DateTimeOffset.UtcNow
            ? at.ToUniversalTime()
            : DateTimeOffset.UtcNow;

        var res = await gameNoticeRepository.AddNotice(
            new()
            {
                Values = [model.Content],
                GameId = game.Id,
                Type = NoticeType.Normal,
                PublishTimeUtc = publishTime
            },
            broadcast: publishTime <= DateTimeOffset.UtcNow,
            token);

        return Ok(res);
    }

    /// <summary>
    /// Get Game Notices
    /// </summary>
    /// <remarks>
    /// Retrieving game notices requires administrator privileges
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game notices</response>
    [RequireGameAdmin]
    [HttpGet("Games/{id:int}/Notices")]
    [ProducesResponseType(typeof(GameNotice[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGameNotices([FromRoute] int id, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        return Ok(await gameNoticeRepository.GetNormalNotices(id, token));
    }

    /// <summary>
    /// Update Game Notice
    /// </summary>
    /// <remarks>
    /// Updating a game notice requires administrator privileges
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="noticeId">Notice ID</param>
    /// <param name="model">Notice content</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully updated notice</response>
    [RequireGameAdmin]
    [HttpPut("Games/{id:int}/Notices/{noticeId:int}")]
    [ProducesResponseType(typeof(GameNotice), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGameNotice([FromRoute] int id, [FromRoute] int noticeId,
        [FromBody] GameNoticeModel model, CancellationToken token = default)
    {
        var notice = await gameNoticeRepository.GetNoticeById(id, noticeId, token);

        if (notice is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Notification_NotFound)],
                StatusCodes.Status404NotFound));

        if (notice.Type != NoticeType.Normal)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Notification_SystemNotEditable)]));

        notice.Values = [model.Content];
        if (model.PublishAt.HasValue)
            notice.PublishTimeUtc = model.PublishAt.Value.ToUniversalTime();
        return Ok(await gameNoticeRepository.UpdateNotice(notice, token));
    }

    /// <summary>
    /// Delete Game Notice
    /// </summary>
    /// <remarks>
    /// Deleting a game notice requires administrator privileges
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="noticeId">Post ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully deleted post</response>
    /// <response code="404">Post not found</response>
    [RequireGameAdmin]
    [HttpDelete("Games/{id:int}/Notices/{noticeId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGameNotice([FromRoute] int id, [FromRoute] int noticeId,
        CancellationToken token)
    {
        var notice = await gameNoticeRepository.GetNoticeById(id, noticeId, token);

        if (notice is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Notification_SystemNotEditable)],
                StatusCodes.Status404NotFound));

        if (notice.Type != NoticeType.Normal)
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Notification_SystemNotDeletable)]));

        await gameNoticeRepository.RemoveNotice(notice, token);

        return Ok();
    }


    /// <summary>
    /// Create Division
    /// </summary>
    /// <remarks>
    /// Add a new division for a game; requires administrator privileges
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="model">Division information</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully created division</response>
    [RequireGameAdmin]
    [HttpPost("Games/{id:int}/Divisions")]
    [ProducesResponseType(typeof(Division), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateDivision([FromRoute] int id, [FromBody] DivisionCreateModel model,
        CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);
        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var division = await divisionRepository.CreateDivision(game, model, token);

        return Ok(division);
    }

    /// <summary>
    /// Get Divisions
    /// </summary>
    /// <remarks>
    /// Retrieve all divisions for a game; requires administrator privileges
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved divisions</response>
    [RequireGameAdmin]
    [HttpGet("Games/{id:int}/Divisions")]
    [ProducesResponseType(typeof(Division[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDivisions([FromRoute] int id, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);
        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var divisions = await divisionRepository.GetDivisions(id, token);
        return Ok(divisions);
    }

    /// <summary>
    /// Update Division
    /// </summary>
    /// <remarks>
    /// Update a division for a game; requires administrator privileges
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="divisionId">Division ID</param>
    /// <param name="model">Division information</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully updated division</response>
    [RequireGameAdmin]
    [HttpPut("Games/{id:int}/Divisions/{divisionId:int}")]
    [ProducesResponseType(typeof(Division), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDivision([FromRoute] int id, [FromRoute] int divisionId,
        [FromBody] DivisionEditModel model, CancellationToken token)
    {
        var division = await divisionRepository.GetDivision(id, divisionId, token);
        if (division is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Division_NotFound)],
                StatusCodes.Status404NotFound));

        await divisionRepository.UpdateDivision(division, model, token);

        return Ok(division);
    }

    /// <summary>
    /// Delete Division
    /// </summary>
    /// <remarks>
    /// Delete a division for a game; requires administrator privileges
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="divisionId">Division ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully deleted division</response>
    [RequireGameAdmin]
    [HttpDelete("Games/{id:int}/Divisions/{divisionId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDivision([FromRoute] int id, [FromRoute] int divisionId,
        CancellationToken token)
    {
        var division = await divisionRepository.GetDivision(id, divisionId, token);
        if (division is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Division_NotFound)],
                StatusCodes.Status404NotFound));

        await divisionRepository.RemoveDivision(division, token);

        return Ok();
    }

    /// <summary>
    /// Add Game Challenge
    /// </summary>
    /// <remarks>
    /// Adding a game challenge requires administrator privileges
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully added game challenge</response>
    [RequireGameAdmin]
    [HttpPost("Games/{id:int}/Challenges")]
    [ProducesResponseType(typeof(ChallengeEditDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddGameChallenge([FromRoute] int id, [FromBody] ChallengeInfoModel model,
        CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var res = await challengeRepository.CreateChallenge(game,
            new GameChallenge { Title = model.Title, Type = model.Type, Category = model.Category }, token);

        return Ok(ChallengeEditDetailModel.FromChallenge(res));
    }

    /// <summary>
    /// Get All Game Challenges
    /// </summary>
    /// <remarks>
    /// Retrieving all game challenges requires administrator privileges
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game challenges</response>
    [RequireGameAdmin]
    [HttpGet("Games/{id:int}/Challenges")]
    [ProducesResponseType(typeof(ChallengeInfoModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGameChallenges([FromRoute] int id, CancellationToken token)
    {
        var challenges = await challengeRepository.GetChallenges(id, token);

        var scoreboard = await gameRepository.TryGetScoreboard(id, token);

        // Hide Pending / Rejected from the main admin list — they live in
        // /admin/games/{id}/pending so the active list stays focused on
        // challenges that participants might actually see.
        var result = challenges
            .Where(c => c.ReviewStatus == ChallengeReviewStatus.Active)
            .Select(c =>
            {
                var model = ChallengeInfoModel.FromChallenge(c);
                if (scoreboard is not null && scoreboard.ChallengeMap.TryGetValue(c.Id, out var challengeInfo))
                    model.Score = challengeInfo.Score;
                return model;
            });

        return Ok(result);
    }

    /// <summary>
    /// Flush Scoreboard Cache
    /// </summary>
    /// <param name="id">Game ID</param>
    /// <param name="token"></param>
    /// <response code="200"></response>
    [RequireGameAdmin]
    [HttpPost("Games/{id:int}/Scoreboard/Flush")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> FlushScoreboardCache([FromRoute] int id, CancellationToken token)
    {
        await cacheHelper.FlushScoreboardCache(id, token);
        return Ok();
    }

    /// <summary>
    /// Get Game Challenge
    /// </summary>
    /// <remarks>
    /// Retrieving a game challenge requires administrator privileges
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="cId">Challenge ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game challenge</response>
    [RequireGameAdmin]
    [HttpGet("Games/{id:int}/Challenges/{cId:int}")]
    [ProducesResponseType(typeof(ChallengeEditDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGameChallenge([FromRoute] int id, [FromRoute] int cId, CancellationToken token)
    {
        var challenge = await challengeRepository.GetChallenge(id, cId, token);

        if (challenge is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        // Do not load flags for dynamic containers
        if (challenge.Type != ChallengeType.DynamicContainer)
            await challengeRepository.LoadFlags(challenge, token);

        var result = ChallengeEditDetailModel.FromChallenge(challenge);
        var scoreboard = await gameRepository.TryGetScoreboard(id, token);

        if (scoreboard is not null && scoreboard.ChallengeMap.TryGetValue(cId, out var challengeInfo))
            result.AcceptedCount = challengeInfo.SolvedCount;

        return Ok(result);
    }

    /// <summary>
    /// Update Game Challenge Information
    /// </summary>
    /// <remarks>
    /// Updating a game challenge, requires administrator privileges. Flags are not affected; use Flag-related APIs to modify
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="cId">Challenge ID</param>
    /// <param name="model">Challenge information</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully updated game challenge</response>
    [RequireGameAdmin]
    [HttpPut("Games/{id:int}/Challenges/{cId:int}")]
    [ProducesResponseType(typeof(ChallengeEditDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGameChallenge([FromRoute] int id, [FromRoute] int cId,
        [FromBody] ChallengeUpdateModel model, CancellationToken token)
    {
        await using var transaction = await challengeRepository.BeginTransactionAsync(token);

        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var res = await challengeRepository.GetChallenge(id, cId, token);

        if (res is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        // NOTE: IsEnabled can only be updated outside the edit page
        if (model.IsEnabled is true && !res.IsEnabled && res.Type != ChallengeType.DynamicContainer)
        {
            await challengeRepository.LoadFlags(res, token);

            if (res.Flags.Count == 0)
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NoFlag)]));
        }

        if (model.EnableTrafficCapture is true && !res.Type.IsContainer())
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Challenge_CaptureNotAllowed)]));

        if (model.FileName is not null && string.IsNullOrWhiteSpace(model.FileName))
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Challenge_DynamicAssetsNotNullable)]));

        var hintUpdated = model.IsHintUpdated(res.Hints?.GetSetHashCode());

        if (!string.IsNullOrWhiteSpace(model.FlagTemplate) && res.Type == ChallengeType.DynamicContainer &&
            !model.IsValidFlagTemplate())
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Challenge_FlagTooTrivial)]));

        res.Update(model);

        switch (model.IsEnabled)
        {
            case true:
                {
                    // Will also update IsEnabled
                    await challengeRepository.EnsureInstances(res, game, token);

                    if (game.IsActive)
                        await gameNoticeRepository.AddNotice(
                            new() { Game = game, Type = NoticeType.NewChallenge, Values = [res.Title] }, broadcast: true, token);
                    break;
                }
            case false when res.Type.IsContainer():
                await instanceRepository.DestroyAllContainers(res, token);
                break;
            case null:
                // do nothing
                break;
        }

        if (game.IsActive && res.IsEnabled && hintUpdated)
            await gameNoticeRepository.AddNotice(
                new() { Game = game, Type = NoticeType.NewHint, Values = [res.Title] },
                broadcast: true, token);

        await challengeRepository.SaveAsync(token);

        await transaction.CommitAsync(token);

        // Always flush scoreboard
        await cacheHelper.FlushScoreboardCache(game.Id, token);

        return Ok(ChallengeEditDetailModel.FromChallenge(res));
    }

    /// <summary>
    /// Test Game Challenge Container
    /// </summary>
    /// <remarks>
    /// Testing a game challenge container requires administrator privileges
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="cId">Challenge ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully started game challenge container</response>
    [RequireGameAdmin]
    [HttpPost("Games/{id:int}/Challenges/{cId:int}/Container")]
    [ProducesResponseType(typeof(ContainerInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTestContainer([FromRoute] int id, [FromRoute] int cId,
        CancellationToken token)
    {
        var challenge = await challengeRepository.GetChallenge(id, cId, token);

        if (challenge is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        if (!challenge.Type.IsContainer())
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerCreationNotAllowed)]));

        if (challenge.ContainerImage is null || challenge.ExposePort is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Container_ConfigError)]));

        var user = await userManager.GetUserAsync(User);

        var container = await containerService.CreateContainerAsync(
            new()
            {
                TeamId = "admin",
                UserId = user!.Id,
                ChallengeId = challenge.Id,
                GameId = challenge.GameId,
                Flag = challenge.Type.IsDynamic() ? challenge.GenerateTestFlag() : null,
                Image = challenge.ContainerImage,
                CPUCount = challenge.CPUCount ?? 1,
                MemoryLimit = challenge.MemoryLimit ?? 64,
                StorageLimit = challenge.StorageLimit ?? 256,
                NetworkMode = challenge.NetworkMode ?? NetworkMode.Open,
                ExposedPort = challenge.ExposePort.Value,
            }, token);

        if (container is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Container_CreationFailed)]));

        challenge.TestContainer = container;
        await challengeRepository.SaveAsync(token);

        logger.Log(
            StaticLocalizer[nameof(Resources.Program.Container_TestContainerCreated), container.LogId],
            user,
            TaskStatus.Success);

        return Ok(ContainerInfoModel.FromContainer(container));
    }

    /// <summary>
    /// Destroy Test Game Challenge Container
    /// </summary>
    /// <remarks>
    /// Destroying a test game challenge container requires administrator privileges
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="cId">Challenge ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully destroyed game challenge container</response>
    [RequireGameAdmin]
    [HttpDelete("Games/{id:int}/Challenges/{cId:int}/Container")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DestroyTestContainer([FromRoute] int id, [FromRoute] int cId,
        CancellationToken token)
    {
        var challenge = await challengeRepository.GetChallenge(id, cId, token);

        if (challenge is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        if (challenge.TestContainer is null)
            return Ok();

        await containerRepository.DestroyContainer(challenge.TestContainer, token);

        return Ok();
    }

    /// <summary>
    /// Delete Game Challenge
    /// </summary>
    /// <remarks>
    /// Deleting a game challenge requires administrator privileges
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="cId">Challenge ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully deleted game challenge</response>
    [RequireGameAdmin]
    [HttpDelete("Games/{id:int}/Challenges/{cId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveGameChallenge([FromRoute] int id, [FromRoute] int cId,
        CancellationToken token)
    {
        var res = await challengeRepository.GetChallenge(id, cId, token);

        if (res is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        await challengeRepository.RemoveChallenge(res, true, token);

        // Always flush scoreboard
        await cacheHelper.FlushScoreboardCache(id, token);

        return Ok();
    }

    /// <summary>
    /// Update Game Challenge Attachment
    /// </summary>
    /// <remarks>
    /// Updating a game challenge attachment requires administrator privileges; only for non-dynamic attachment challenges
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="cId">Challenge ID</param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully updated game challenge</response>
    [RequireGameAdmin]
    [HttpPost("Games/{id:int}/Challenges/{cId:int}/Attachment")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAttachment([FromRoute] int id, [FromRoute] int cId,
        [FromBody] AttachmentCreateModel model, CancellationToken token)
    {
        var challenge = await challengeRepository.GetChallenge(id, cId, token);

        if (challenge is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        if (challenge.Type == ChallengeType.DynamicAttachment)
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Challenge_UseAssetsApiForDynamic)]));

        await challengeRepository.UpdateAttachment(challenge, model, token);

        return Ok();
    }

    /// <summary>
    /// Add Game Challenge Flag
    /// </summary>
    /// <remarks>
    /// Adding a game challenge flag requires administrator privileges
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="cId">Challenge ID</param>
    /// <param name="models"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully added game challenge flags</response>
    [RequireGameAdmin]
    [HttpPost("Games/{id:int}/Challenges/{cId:int}/Flags")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddFlags([FromRoute] int id, [FromRoute] int cId,
        [FromBody] FlagCreateModel[] models, CancellationToken token)
    {
        var challenge = await challengeRepository.GetChallenge(id, cId, token);

        if (challenge is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        await challengeRepository.AddFlags(challenge, models, token);

        return Ok();
    }

    /// <summary>
    /// Delete Game Challenge Flag
    /// </summary>
    /// <remarks>
    /// Deleting a game challenge flag requires administrator privileges
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="cId">Challenge ID</param>
    /// <param name="fId">Flag ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully deleted game challenge flag</response>
    [RequireGameAdmin]
    [HttpDelete("Games/{id:int}/Challenges/{cId:int}/Flags/{fId:int}")]
    [ProducesResponseType(typeof(TaskStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFlag([FromRoute] int id, [FromRoute] int cId, [FromRoute] int fId,
        CancellationToken token)
    {
        var challenge = await challengeRepository.GetChallenge(id, cId, token);

        if (challenge is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        return Ok(await challengeRepository.RemoveFlag(challenge, fId, token));
    }


    /// <summary>
    /// Export game package
    /// </summary>
    /// <remarks>
    /// Export game with all challenges, divisions, and attachments as a ZIP file; requires Admin permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully exported game package</response>
    /// <response code="400">Invalid operation</response>
    /// <response code="404">Game not found</response>
    /// <response code="500">Internal server error during export</response>
    [RequireGameAdmin]
    [HttpPost("Games/{id:int}/Export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportGame([FromRoute] int id, CancellationToken token = default)
    {
        try
        {
            var result = await exportService.ExportGameAsync(id, token);

            if (result is null)
            {
                logger.SystemLog(
                    StaticLocalizer[nameof(Resources.Program.Game_NotFound)],
                    TaskStatus.NotFound,
                    LogLevel.Warning);
                return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                    StatusCodes.Status404NotFound));
            }

            var fileName = $"{result.Game.Title}-export-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.zip";

            logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.Game_Exported), result.Game.Title, fileName]);

            var fileStream = new FileStream(
                result.ZipFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.DeleteOnClose | FileOptions.SequentialScan | FileOptions.Asynchronous);

            return File(fileStream, "application/zip", fileName, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.Game_ExportFailed), id],
                TaskStatus.Failed,
                LogLevel.Error);
            logger.LogError(ex, "Failed to export game {GameId}", id);
            return RequestResponse.Result(localizer[nameof(Resources.Program.Error_InternalServerError)],
                StatusCodes.Status500InternalServerError);
        }
    }

    private static readonly string[] AllowedImportContentTypes = ["application/zip", "application/x-zip-compressed"];

    /// <summary>
    /// Import game package
    /// </summary>
    /// <remarks>
    /// Import game from a ZIP package; requires Admin permission
    /// </remarks>
    /// <param name="file">Game package ZIP file</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully imported game, returns game ID</response>
    /// <response code="400">Invalid package or import failed</response>
    /// <response code="500">Internal server error during import</response>
    [RequireAdmin]
    [HttpPost("Games/Import")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status500InternalServerError)]
    [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> ImportGame(IFormFile file, CancellationToken token = default)
    {
        switch (file.Length)
        {
            case 0:
                logger.SystemLog(
                    StaticLocalizer[nameof(Resources.Program.File_SizeZero)],
                    TaskStatus.Failed,
                    LogLevel.Warning);
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_SizeZero)]));
            case > 512 * 1024 * 1024:
                // 512MB limit
                logger.SystemLog(
                    StaticLocalizer[nameof(Resources.Program.File_SizeTooLarge), file.FileName],
                    TaskStatus.Failed,
                    LogLevel.Warning);
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_SizeTooLarge)]));
        }

        if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
            !AllowedImportContentTypes.Contains(file.ContentType))
        {
            logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.File_TypeNotSupported), file.FileName],
                TaskStatus.Failed,
                LogLevel.Warning);
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_TypeNotSupported)]));
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var gameId = await importService.ImportGameAsync(stream, token);

            if (gameId is null)
            {
                logger.SystemLog(
                    StaticLocalizer[nameof(Resources.Program.Game_ImportFailed), file.FileName],
                    TaskStatus.Failed,
                    LogLevel.Warning);
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_ImportFailed)]));
            }

            await cacheHelper.FlushRecentGamesCache(token);

            logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.Game_Imported), gameId, file.FileName]);

            return Ok(gameId);
        }
        catch (InvalidOperationException)
        {
            logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.Game_ImportInvalidPackage), file.FileName],
                TaskStatus.Failed,
                LogLevel.Warning);
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_ImportInvalidPackage)]));
        }
        catch (Exception ex)
        {
            logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.Game_ImportFailed), file.FileName],
                TaskStatus.Failed,
                LogLevel.Error);
            logger.LogError(ex, "Failed to import game from file {FileName}: {ErrorMessage}", file.FileName,
                ex.Message);
            return RequestResponse.Result(localizer[nameof(Resources.Program.Error_InternalServerError)],
                StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get Challenge Reviews
    /// </summary>
    /// <remarks>
    /// Retrieving challenge reviews requires administrator privileges
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="search"></param>
    /// <param name="rating"></param>
    /// <param name="repository"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved challenge reviews</response>
    [RequireGameAdmin]
    [HttpGet("Games/{id:int}/Reviews")]
    [ProducesResponseType(typeof(ArrayResponse<ChallengeReviewDetailModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReviews([FromRoute] int id, [FromQuery][Range(0, 100)] int count = 20, [FromQuery] int skip = 0,
        [FromQuery] string? search = null, [FromQuery] ReviewRating? rating = null, [FromServices] IChallengeReviewRepository repository = null!, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var reviews = await repository.GetReviewsAsync(id, skip, count, search, rating, token);
        var total = await repository.GetReviewCountAsync(id, search, rating, token);

        return Ok(reviews.Select(ChallengeReviewDetailModel.FromReview).ToResponse(total));
    }

    /// <summary>
    /// Get Challenge Review Analytics
    /// </summary>
    /// <param name="id">Game ID</param>
    /// <param name="repository"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved analytics</response>
    [RequireGameAdmin]
    [HttpGet("Games/{id:int}/Reviews/Analytics")]
    [ProducesResponseType(typeof(ReviewAnalyticsModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReviewAnalytics([FromRoute] int id,
        [FromServices] IChallengeReviewRepository repository, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var analytics = await repository.GetAnalyticsAsync(id, token);

        return Ok(analytics);
    }

    /// <summary>
    /// Get Event Admins
    /// </summary>
    /// <remarks>
    /// Get all event admins for a game; requires Admin permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="dbContext"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved event admins</response>
    [HttpGet("Games/{id:int}/Admins")]
    [RequireAdmin]
    [ProducesResponseType(typeof(UserInfoModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventAdmins([FromRoute] int id, [FromServices] AppDbContext dbContext, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);
        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var admins = await dbContext.EventManagers
            .Where(e => e.GameId == id)
            .Include(e => e.User)
            .Select(e => e.User)
            .ToArrayAsync(token);

        return Ok(admins.Select(UserInfoModel.FromUserInfo));
    }

    /// <summary>
    /// Add Event Admin
    /// </summary>
    /// <remarks>
    /// Add an event admin for a game; requires Admin permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="dbContext"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully added event admin</response>
    [HttpPost("Games/{id:int}/Admins/{userId:guid}")]
    [RequireAdmin]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddEventAdmin([FromRoute] int id, [FromRoute] Guid userId, [FromServices] AppDbContext dbContext, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);
        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Admin_UserNotFound)],
                StatusCodes.Status404NotFound));
        
        if (await dbContext.EventManagers.AnyAsync(e => e.GameId == id && e.UserId == userId, token))
            return Ok();

        dbContext.EventManagers.Add(new EventManager { GameId = id, UserId = userId });
        await dbContext.SaveChangesAsync(token);

        return Ok();
    }

    /// <summary>
    /// Remove Event Admin
    /// </summary>
    /// <remarks>
    /// Remove an event admin from a game; requires Admin permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="dbContext"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully removed event admin</response>
    [HttpDelete("Games/{id:int}/Admins/{userId:guid}")]
    [RequireAdmin]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveEventAdmin([FromRoute] int id, [FromRoute] Guid userId, [FromServices] AppDbContext dbContext, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);
        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var manager = await dbContext.EventManagers.FirstOrDefaultAsync(e => e.GameId == id && e.UserId == userId, token);
        if (manager is null)
             return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Admin_UserNotFound)],
                StatusCodes.Status404NotFound));

        dbContext.EventManagers.Remove(manager);
        await dbContext.SaveChangesAsync(token);

        return Ok();
    }

    // =========================================================
    //  Challenge import / review / repo-watch endpoints
    //  See /root/.claude/plans/compiled-squishing-neumann.md
    // =========================================================

    const long MaxTarballBytes = 64L * 1024 * 1024;

    /// <summary>
    /// Submit a single-challenge tarball for admin review. Any logged-in
    /// user may call this; the challenge lands with
    /// <see cref="ChallengeReviewStatus.Pending"/> and is hidden from
    /// participants until an admin approves.
    /// </summary>
    [RequireUser]
    [HttpPost("Games/{id:int}/Challenges/Submit")]
    [RequestSizeLimit(MaxTarballBytes)]
    [ProducesResponseType(typeof(ChallengeImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitChallenge(
        [FromRoute] int id, IFormFile archive, CancellationToken token)
    {
        if (archive is null || archive.Length == 0)
            return BadRequest(new RequestResponse("Archive is required."));
        if (archive.Length > MaxTarballBytes)
            return BadRequest(new RequestResponse("Archive exceeds 64 MB."));

        var game = await dbContext.Games.FirstOrDefaultAsync(g => g.Id == id, token);
        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        // Per-game gate. Admin / game-admin still bypass this via the
        // Import endpoint above; this only restricts the public Submit path.
        if (!game.AllowUserSubmissions)
            return new ObjectResult(new RequestResponse("User submissions are disabled for this game.",
                StatusCodes.Status403Forbidden))
            { StatusCode = StatusCodes.Status403Forbidden };

        var user = (await userManager.GetUserAsync(User))!;

        await using var stream = archive.OpenReadStream();
        var result = await challengeImportService.ImportFromArchiveAsync(
            stream, new ChallengeImportOptions(id, user.Id, AutoApprove: false), token);
        return Ok(result);
    }

    /// <summary>
    /// Admin / game-admin one-shot tarball import; auto-approves the
    /// resulting challenges.
    /// </summary>
    [RequireGameAdmin]
    [HttpPost("Games/{id:int}/Challenges/Import")]
    [RequestSizeLimit(MaxTarballBytes)]
    [ProducesResponseType(typeof(ChallengeImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportChallenge(
        [FromRoute] int id, IFormFile archive, CancellationToken token)
    {
        if (archive is null || archive.Length == 0)
            return BadRequest(new RequestResponse("Archive is required."));

        var user = (await userManager.GetUserAsync(User))!;

        await using var stream = archive.OpenReadStream();
        var result = await challengeImportService.ImportFromArchiveAsync(
            stream, new ChallengeImportOptions(id, user.Id, AutoApprove: true), token);
        return Ok(result);
    }

    /// <summary>
    /// One-shot bulk import from a github repo. Admin / game-admin only;
    /// regular users can only submit single-challenge archives.
    /// </summary>
    [RequireGameAdmin]
    [HttpPost("Games/{id:int}/Challenges/ImportFromGitHub")]
    [ProducesResponseType(typeof(ChallengeImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportChallengeFromGitHub(
        [FromRoute] int id, [FromBody] ImportFromGitHubModel model, CancellationToken token)
    {
        if (!GitHubLocator.TryParse(model.RepoUrl, model.Ref, model.Subpath, out var loc, out var err) || loc is null)
            return BadRequest(new RequestResponse(err ?? "Invalid github URL."));

        var user = (await userManager.GetUserAsync(User))!;
        var result = await challengeImportService.ImportFromGitHubAsync(
            loc, model.GitHubToken, new ChallengeImportOptions(id, user.Id, AutoApprove: true), token);
        return Ok(result);
    }

    /// <summary>
    /// List challenges awaiting admin review for this game.
    /// </summary>
    [RequireGameAdmin]
    [HttpGet("Games/{id:int}/PendingChallenges")]
    [ProducesResponseType(typeof(PendingChallengeModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPendingChallenges([FromRoute] int id, CancellationToken token)
    {
        // Pending + Rejected both belong to the review queue. Active rows
        // live in the main /admin/games/{id}/challenges list; Active is
        // excluded here so the queue stays focused on "needs attention".
        var rows = await dbContext.GameChallenges
            .AsNoTracking()
            .Where(c => c.GameId == id && c.ReviewStatus != ChallengeReviewStatus.Active)
            .OrderBy(c => c.ReviewStatus) // Pending (1) before Rejected (2)
            .ThenByDescending(c => c.SubmittedAtUtc)
            .Select(c => new PendingChallengeModel
            {
                Id = c.Id,
                Title = c.Title,
                Category = c.Category,
                Type = c.Type,
                ReviewStatus = c.ReviewStatus,
                ReviewNote = c.ReviewNote,
                SubmittedAtUtc = c.SubmittedAtUtc,
                ReviewedAtUtc = c.ReviewedAtUtc,
                SubmittedByUserId = c.SubmittedByUserId,
                SubmittedByUserName = c.SubmittedByUserId != null
                    ? dbContext.Users.Where(u => u.Id == c.SubmittedByUserId).Select(u => u.UserName).FirstOrDefault()
                    : null
            })
            .ToArrayAsync(token);
        return Ok(rows);
    }

    /// <summary>
    /// Approve a pending challenge. Optionally enables it in one shot.
    /// </summary>
    [RequireGameAdmin]
    [HttpPost("Games/{id:int}/Challenges/{cId:int}/Approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveChallenge(
        [FromRoute] int id, [FromRoute] int cId, CancellationToken token)
    {
        var challenge = await dbContext.GameChallenges
            .FirstOrDefaultAsync(c => c.GameId == id && c.Id == cId, token);
        if (challenge is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        challenge.ReviewStatus = ChallengeReviewStatus.Active;
        challenge.ReviewedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(token);
        return Ok();
    }

    /// <summary>
    /// Reject a pending (or active) challenge. Persists the optional note
    /// for audit. Challenge stays in the DB but is hidden from participants.
    /// </summary>
    [RequireGameAdmin]
    [HttpPost("Games/{id:int}/Challenges/{cId:int}/Reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectChallenge(
        [FromRoute] int id, [FromRoute] int cId,
        [FromBody] RejectChallengeModel model, CancellationToken token)
    {
        var challenge = await dbContext.GameChallenges
            .FirstOrDefaultAsync(c => c.GameId == id && c.Id == cId, token);
        if (challenge is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        challenge.ReviewStatus = ChallengeReviewStatus.Rejected;
        challenge.ReviewedAtUtc = DateTimeOffset.UtcNow;
        challenge.ReviewNote = model.Note;
        await dbContext.SaveChangesAsync(token);
        return Ok();
    }

    /// <summary>
    /// Re-run the auto-build pipeline against the persisted original
    /// archive. Useful when a Dockerfile change shipped and the admin
    /// wants to refresh the image without re-uploading the package.
    /// 404 if the challenge has no original archive on file (e.g.
    /// admin-created or github-sourced with no blob saved). Returns the
    /// fresh <see cref="ChallengeAuditModel"/> so the UI can refresh
    /// the build-status badge inline.
    /// </summary>
    [RequireGameAdmin]
    [HttpPost("Games/{id:int}/Challenges/{cId:int}/Rebuild")]
    [ProducesResponseType(typeof(Models.Response.Admin.ChallengeAuditModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RebuildChallengeImage(
        [FromRoute] int id, [FromRoute] int cId,
        [FromServices] Services.Container.Build.IChallengeImageBuilder builder,
        [FromServices] Storage.Interface.IBlobStorage storage,
        CancellationToken token)
    {
        var challenge = await dbContext.GameChallenges
            .FirstOrDefaultAsync(c => c.GameId == id && c.Id == cId, token);
        if (challenge is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)]));
        if (string.IsNullOrEmpty(challenge.OriginalArchiveBlobPath))
            return NotFound(new RequestResponse(
                "No archive on file for this challenge — re-upload to trigger a build.",
                StatusCodes.Status404NotFound));
        if (!await storage.ExistsAsync(challenge.OriginalArchiveBlobPath, token))
            return NotFound(new RequestResponse(
                "Archive blob is missing from storage.", StatusCodes.Status404NotFound));

        // Extract the archive into a temp dir, mirror the import service's
        // approach: spool first, then dispatch on magic bytes.
        var workDir = Path.Combine(Path.GetTempPath(), $"gzctf-rebuild-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workDir);
        challenge.BuildStatus = ChallengeBuildStatus.Building;
        await dbContext.SaveChangesAsync(token);
        try
        {
            await using (var src = await storage.OpenReadAsync(challenge.OriginalArchiveBlobPath, token))
            {
                var spool = Path.Combine(workDir, "__archive.bin");
                await using (var fs = System.IO.File.Create(spool))
                    await src.CopyToAsync(fs, token);
                await using var sf = System.IO.File.OpenRead(spool);
                await Services.Transfer.ChallengeImportService.ExtractArchiveAsync(sf, workDir, token);
                System.IO.File.Delete(spool);
            }

            // The archive may be nested under a wrapping directory (e.g.
            // github tarballs). Pick the first dir entry as the package
            // root if there's exactly one — same logic the import service
            // and the audit modal use.
            var topLevel = Directory.EnumerateFileSystemEntries(workDir).Take(2).ToArray();
            var packageDir = topLevel.Length == 1 && Directory.Exists(topLevel[0])
                ? topLevel[0]
                : workDir;

            // We don't re-parse challenge.yaml here — the challenge row
            // already carries ContainerImage. Honor it: if it's a local
            // path style, rebuild from the inferred context; if it's a
            // registry ref, this endpoint is a no-op (return current
            // state). Inferring the context is identical to what
            // ResolveBuildContext did at import time.
            var declared = challenge.ContainerImage?.Trim();
            if (string.IsNullOrEmpty(declared) ||
                !(declared.StartsWith("./") || declared.StartsWith("../") || declared.StartsWith('/') ||
                  declared.Equals("Dockerfile", StringComparison.OrdinalIgnoreCase) ||
                  declared.EndsWith("/Dockerfile", StringComparison.OrdinalIgnoreCase) ||
                  declared.StartsWith("gzctf-auto/", StringComparison.OrdinalIgnoreCase)))
            {
                // Not a local-build challenge. Restore the previous state
                // (no longer "Building") and surface a clear message.
                challenge.BuildStatus = ChallengeBuildStatus.None;
                challenge.LastBuildLog =
                    "Rebuild skipped: this challenge ships a published registry image. Re-upload to change.";
                await dbContext.SaveChangesAsync(token);
                return NotFound(new RequestResponse(
                    "Rebuild is only valid for challenges with a local Dockerfile.",
                    StatusCodes.Status404NotFound));
            }

            // For previously-built challenges (declared == "gzctf-auto/...")
            // we need to find a Dockerfile in the package — same heuristic
            // as on import: prefer src/, fall back to package root.
            string contextDir;
            string dockerfile;
            if (declared.StartsWith("./") || declared.StartsWith("../") || declared.StartsWith('/'))
            {
                var rel = declared.Replace('\\', '/').TrimStart('.').TrimStart('/');
                var combined = Path.Combine(packageDir, rel);
                if (Directory.Exists(combined)) { contextDir = Path.GetFullPath(combined); dockerfile = "Dockerfile"; }
                else if (System.IO.File.Exists(combined)) { contextDir = Path.GetFullPath(Path.GetDirectoryName(combined)!); dockerfile = "Dockerfile"; }
                else { contextDir = Path.GetFullPath(combined); dockerfile = "Dockerfile"; }
            }
            else if (declared.Equals("Dockerfile", StringComparison.OrdinalIgnoreCase))
            {
                contextDir = Path.GetFullPath(packageDir); dockerfile = "Dockerfile";
            }
            else
            {
                // gzctf-auto tag — look for a Dockerfile in src/ first, then root.
                var srcDir = Path.Combine(packageDir, "src");
                if (System.IO.File.Exists(Path.Combine(srcDir, "Dockerfile")))
                {
                    contextDir = Path.GetFullPath(srcDir); dockerfile = "Dockerfile";
                }
                else
                {
                    contextDir = Path.GetFullPath(packageDir); dockerfile = "Dockerfile";
                }
            }

            if (!System.IO.File.Exists(Path.Combine(contextDir, dockerfile)))
            {
                challenge.BuildStatus = ChallengeBuildStatus.Failed;
                challenge.LastBuildLog = $"Rebuild failed: no Dockerfile found at '{contextDir}/{dockerfile}'.";
                await dbContext.SaveChangesAsync(token);
                return BadRequest(new RequestResponse(challenge.LastBuildLog));
            }

            var result = await builder.BuildAsync(
                new Services.Container.Build.ChallengeBuildRequest(id, challenge.Title, contextDir, dockerfile),
                token);

            challenge.BuildStatus = result.Success ? ChallengeBuildStatus.Success : ChallengeBuildStatus.Failed;
            challenge.LastBuildLog = result.LogTail;
            if (result.Success && result.ImageTag is not null)
            {
                challenge.ContainerImage = result.ImageTag;
                challenge.BuildImageDigest = result.Digest;
            }
            await dbContext.SaveChangesAsync(token);

            return Ok(new Models.Response.Admin.ChallengeAuditModel
            {
                ArchiveAvailable = true,
                BuildStatus = challenge.BuildStatus,
                LastBuildLog = challenge.LastBuildLog
            });
        }
        catch (Exception e)
        {
            challenge.BuildStatus = ChallengeBuildStatus.Failed;
            challenge.LastBuildLog = $"Rebuild crashed: {e.Message}";
            await dbContext.SaveChangesAsync(token);
            return BadRequest(new RequestResponse(challenge.LastBuildLog));
        }
        finally
        {
            try { Directory.Delete(workDir, recursive: true); } catch { /* best effort */ }
        }
    }

    /// <summary>
    /// Download the original archive that produced a challenge, for
    /// offline audit. Returns 404 when no archive was persisted
    /// (admin-created or github-sourced).
    /// </summary>
    [RequireGameAdmin]
    [HttpGet("Games/{id:int}/Challenges/{cId:int}/AuditArchive")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChallengeAuditArchive(
        [FromRoute] int id, [FromRoute] int cId, CancellationToken token,
        [FromServices] IBlobStorage storage)
    {
        var challenge = await dbContext.GameChallenges.AsNoTracking()
            .FirstOrDefaultAsync(c => c.GameId == id && c.Id == cId, token);
        if (challenge?.OriginalArchiveBlobPath is null)
            return NotFound(new RequestResponse("No archive available for this challenge."));

        if (!await storage.ExistsAsync(challenge.OriginalArchiveBlobPath, token))
            return NotFound(new RequestResponse("Archive blob is missing from storage."));

        var stream = await storage.OpenReadAsync(challenge.OriginalArchiveBlobPath, token);
        var safeTitle = string.Concat(challenge.Title
            .Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_'));
        return File(stream, "application/octet-stream", $"{safeTitle}.archive.bin");
    }

    /// <summary>
    /// Parse the stored archive on-demand and return YAML text, file
    /// tree, and previews of reviewer-targeted files (READMEs / writeups
    /// / solvers). Heavy enough that the modal calls it explicitly on
    /// open, not on every render.
    /// </summary>
    [RequireGameAdmin]
    [HttpGet("Games/{id:int}/Challenges/{cId:int}/AuditMeta")]
    [ProducesResponseType(typeof(Models.Response.Admin.ChallengeAuditModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChallengeAuditMeta(
        [FromRoute] int id, [FromRoute] int cId, CancellationToken token,
        [FromServices] IBlobStorage storage)
    {
        var challenge = await dbContext.GameChallenges.AsNoTracking()
            .FirstOrDefaultAsync(c => c.GameId == id && c.Id == cId, token);
        if (challenge is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)]));

        var model = new Models.Response.Admin.ChallengeAuditModel
        {
            ArchiveAvailable = challenge.OriginalArchiveBlobPath is not null,
            BuildStatus = challenge.BuildStatus,
            LastBuildLog = challenge.LastBuildLog
        };

        if (challenge.OriginalArchiveBlobPath is null)
            return Ok(model);

        if (!await storage.ExistsAsync(challenge.OriginalArchiveBlobPath, token))
        {
            model.ArchiveAvailable = false;
            return Ok(model);
        }

        var tempDir = Path.Combine(Path.GetTempPath(), $"gzctf-audit-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            await using (var src = await storage.OpenReadAsync(challenge.OriginalArchiveBlobPath, token))
            {
                var spool = Path.Combine(tempDir, "__archive.bin");
                await using (var fs = System.IO.File.Create(spool))
                    await src.CopyToAsync(fs, token);
                await using var sf = System.IO.File.OpenRead(spool);
                await ChallengeImportService.ExtractArchiveAsync(sf, tempDir, token);
                System.IO.File.Delete(spool);
            }

            FillAuditModel(model, tempDir);
            return Ok(model);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Audit: failed to parse archive for challenge {Id}", cId);
            return Ok(model);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best effort */ }
        }
    }

    private static void FillAuditModel(Models.Response.Admin.ChallengeAuditModel model, string root)
    {
        var rootCanonical = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        var files = new List<Models.Response.Admin.ChallengeAuditFile>();

        // GitHub-style downloads wrap content in one dir; descend if so.
        var firstLevel = Directory.EnumerateFileSystemEntries(root).Take(2).ToArray();
        var scanRoot = firstLevel.Length == 1 && Directory.Exists(firstLevel[0])
            ? firstLevel[0]
            : root;
        var scanCanonical = Path.GetFullPath(scanRoot) + Path.DirectorySeparatorChar;

        // First yaml we find wins. Anything deeper is also collected as a file
        // entry — admins still see it but it doesn't dominate the panel.
        string? yamlText = null;
        var previews = new Dictionary<string, string>();
        var previewKeywords = new[] { "readme", "writeup", "solution", "solve", "solver", "notes" };

        foreach (var path in Directory.EnumerateFiles(scanRoot, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(scanRoot, path).Replace('\\', '/');
            var info = new FileInfo(path);
            files.Add(new() { Path = rel, Size = info.Length });

            var name = Path.GetFileName(path);
            if (yamlText is null &&
                (string.Equals(name, "challenge.yaml", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(name, "challenge.yml", StringComparison.OrdinalIgnoreCase)))
            {
                try { yamlText = System.IO.File.ReadAllText(path); }
                catch { /* ignore */ }
                continue;
            }

            var lowerName = name.ToLowerInvariant();
            if (info.Length <= 64 * 1024 &&
                previewKeywords.Any(k => lowerName.Contains(k)))
            {
                try
                {
                    var contents = System.IO.File.ReadAllText(path);
                    if (contents.Length > 8 * 1024)
                        contents = contents[..(8 * 1024)] + "\n…(truncated)";
                    previews[rel] = contents;
                }
                catch { /* binary or unreadable; skip */ }
            }
        }

        model.YamlText = yamlText;
        model.Files = files.OrderBy(f => f.Path, StringComparer.OrdinalIgnoreCase).ToArray();
        model.Previews = previews;
    }

    /// <summary>
    /// When the game was auto-spawned by a global
    /// <see cref="GZCTF.Models.Data.GameRepoBinding"/>, return read-only
    /// details about which binding owns it and on what cadence it
    /// re-polls. Returns <c>null</c> when the game is hand-authored —
    /// the watches page uses that as the signal to render the regular
    /// add-watch form. No per-game <see cref="GZCTF.Models.Data.RepoWatch"/>
    /// row is created for binding-owned games (single-authoritative
    /// poller, no double-scan); this endpoint is what surfaces the
    /// binding to the per-game UI.
    /// </summary>
    [RequireGameAdmin]
    [HttpGet("Games/{id:int}/WatchBinding")]
    [ProducesResponseType(typeof(GameWatchBindingModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGameWatchBinding([FromRoute] int id, CancellationToken token)
    {
        var info = await dbContext.Games.AsNoTracking()
            .Where(g => g.Id == id && g.RepoBindingId != null)
            .Select(g => new
            {
                g.EventManifestPath,
                Binding = dbContext.GameRepoBindings.AsNoTracking()
                    .Where(b => b.Id == g.RepoBindingId)
                    .Select(b => new
                    {
                        b.Id,
                        b.RepoUrl,
                        b.Ref,
                        b.IntervalSeconds,
                        b.Status,
                        b.TokenStatus,
                        b.LastScanUtc,
                        b.NextScanUtc,
                        b.LastScanMessage
                    })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(token);

        if (info?.Binding is null)
            return Ok((GameWatchBindingModel?)null);

        return Ok(new GameWatchBindingModel
        {
            BindingId = info.Binding.Id,
            RepoUrl = info.Binding.RepoUrl,
            Ref = info.Binding.Ref,
            EventManifestPath = info.EventManifestPath,
            IntervalSeconds = info.Binding.IntervalSeconds,
            Status = info.Binding.Status,
            TokenStatus = info.Binding.TokenStatus,
            LastScanUtc = info.Binding.LastScanUtc,
            NextScanUtc = info.Binding.NextScanUtc,
            LastScanMessage = info.Binding.LastScanMessage
        });
    }

    /// <summary>
    /// List configured repo watches for this game with last sync info.
    /// </summary>
    [RequireGameAdmin]
    [HttpGet("Games/{id:int}/Watches")]
    [ProducesResponseType(typeof(RepoWatchInfoModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRepoWatches([FromRoute] int id, CancellationToken token)
    {
        var rows = await dbContext.RepoWatches.AsNoTracking()
            .Where(w => w.GameId == id)
            .OrderByDescending(w => w.CreatedAtUtc)
            .Select(w => new RepoWatchInfoModel
            {
                Id = w.Id,
                RepoUrl = w.RepoUrl,
                Ref = w.Ref,
                Subpath = w.Subpath,
                IntervalSeconds = w.IntervalSeconds,
                Status = w.Status,
                NextRunUtc = w.NextRunUtc,
                LastRunUtc = w.LastRunUtc,
                LastCommitSha = w.LastCommitSha,
                HasGitHubToken = w.GitHubTokenEncrypted != null,
                TokenStatus = w.TokenStatus,
                LastSync = dbContext.RepoWatchSyncs
                    .Where(s => s.RepoWatchId == w.Id)
                    .OrderByDescending(s => s.RanAtUtc)
                    .Select(s => new RepoWatchSyncModel
                    {
                        RanAtUtc = s.RanAtUtc,
                        CommitSha = s.CommitSha,
                        Imported = s.Imported,
                        Updated = s.Updated,
                        Skipped = s.Skipped,
                        Failed = s.Failed,
                        ErrorMessage = s.ErrorMessage
                    })
                    .FirstOrDefault()
            })
            .ToArrayAsync(token);
        return Ok(rows);
    }

    /// <summary>
    /// Create a new repo watch. Validates the URL up front and clamps the
    /// interval; schedules the first run immediately when requested.
    /// </summary>
    [RequireGameAdmin]
    [HttpPost("Games/{id:int}/Watches")]
    [ProducesResponseType(typeof(RepoWatchInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRepoWatch(
        [FromRoute] int id, [FromBody] RepoWatchCreateModel model, CancellationToken token)
    {
        if (!GitHubLocator.TryParse(model.RepoUrl, model.Ref, model.Subpath, out _, out var err))
            return BadRequest(new RequestResponse(err ?? "Invalid github URL."));

        // One repository per event. The watch is conceptually the single
        // source of truth for the game; bidirectional sync (pull + push)
        // relies on a 1:1 mapping. Admin must delete the existing watch
        // before pointing the game at a different repo.
        if (await dbContext.RepoWatches.AnyAsync(w => w.GameId == id, token))
            return Conflict(new RequestResponse(
                "This game already has a repo watch. Delete or update the existing one before adding another.",
                StatusCodes.Status409Conflict));

        // Games auto-created by a GameRepoBinding are managed at the
        // global /admin/repo-bindings page. A per-game watch on top of
        // that would create two competing polling sources.
        var ownedByBinding = await dbContext.Games
            .AsNoTracking()
            .Where(g => g.Id == id)
            .Select(g => g.RepoBindingId)
            .FirstOrDefaultAsync(token);
        if (ownedByBinding is not null)
            return Conflict(new RequestResponse(
                "This game was auto-created by a repo binding — manage it from /admin/repo-bindings instead.",
                StatusCodes.Status409Conflict));

        var user = (await userManager.GetUserAsync(User))!;

        var watch = new RepoWatch
        {
            GameId = id,
            RepoUrl = model.RepoUrl.Trim(),
            Ref = string.IsNullOrWhiteSpace(model.Ref) ? null : model.Ref.Trim(),
            Subpath = string.IsNullOrWhiteSpace(model.Subpath) ? null : model.Subpath.Trim().TrimEnd('/'),
            IntervalSeconds = Math.Clamp(model.IntervalSeconds, 60, 86400),
            Status = RepoWatchStatus.Active,
            NextRunUtc = model.RunImmediately ? DateTimeOffset.UtcNow : DateTimeOffset.UtcNow.AddSeconds(model.IntervalSeconds),
            CreatedByUserId = user.Id,
            GitHubTokenEncrypted = string.IsNullOrWhiteSpace(model.GitHubToken)
                ? null
                : _repoWatchProtector.Protect(model.GitHubToken!.Trim()),
            TokenStatus = string.IsNullOrWhiteSpace(model.GitHubToken)
                ? TokenStatus.NotConfigured
                : TokenStatus.Ok
        };

        dbContext.RepoWatches.Add(watch);
        await dbContext.SaveChangesAsync(token);

        return Ok(new RepoWatchInfoModel
        {
            Id = watch.Id,
            RepoUrl = watch.RepoUrl,
            Ref = watch.Ref,
            Subpath = watch.Subpath,
            IntervalSeconds = watch.IntervalSeconds,
            Status = watch.Status,
            NextRunUtc = watch.NextRunUtc,
            LastRunUtc = watch.LastRunUtc,
            LastCommitSha = watch.LastCommitSha,
            HasGitHubToken = watch.GitHubTokenEncrypted != null,
            TokenStatus = watch.TokenStatus
        });
    }

    /// <summary>
    /// Update an existing repo watch (interval / ref / subpath / pause-resume).
    /// </summary>
    [RequireGameAdmin]
    [HttpPut("Games/{id:int}/Watches/{watchId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRepoWatch(
        [FromRoute] int id, [FromRoute] int watchId,
        [FromBody] RepoWatchUpdateModel model, CancellationToken token)
    {
        var watch = await dbContext.RepoWatches.FirstOrDefaultAsync(
            w => w.Id == watchId && w.GameId == id, token);
        if (watch is null)
            return NotFound(new RequestResponse("Watch not found."));

        if (model.Ref is not null) watch.Ref = string.IsNullOrWhiteSpace(model.Ref) ? null : model.Ref.Trim();
        if (model.Subpath is not null) watch.Subpath = string.IsNullOrWhiteSpace(model.Subpath) ? null : model.Subpath.Trim().TrimEnd('/');
        if (model.IntervalSeconds is { } iv) watch.IntervalSeconds = Math.Clamp(iv, 60, 86400);
        if (model.Status is { } status) watch.Status = status;

        // GitHubToken: null = keep existing, empty = clear, non-empty = re-protect.
        if (model.GitHubToken is not null)
        {
            if (string.IsNullOrWhiteSpace(model.GitHubToken))
            {
                watch.GitHubTokenEncrypted = null;
                watch.TokenStatus = TokenStatus.NotConfigured;
            }
            else
            {
                watch.GitHubTokenEncrypted = _repoWatchProtector.Protect(model.GitHubToken.Trim());
                watch.TokenStatus = TokenStatus.Ok;
            }
        }

        await dbContext.SaveChangesAsync(token);
        return Ok();
    }

    /// <summary>
    /// Delete a repo watch. Does NOT delete the challenges already imported by it.
    /// </summary>
    [RequireGameAdmin]
    [HttpDelete("Games/{id:int}/Watches/{watchId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRepoWatch(
        [FromRoute] int id, [FromRoute] int watchId, CancellationToken token)
    {
        var watch = await dbContext.RepoWatches.FirstOrDefaultAsync(
            w => w.Id == watchId && w.GameId == id, token);
        if (watch is null)
            return NotFound(new RequestResponse("Watch not found."));

        dbContext.RepoWatches.Remove(watch);
        await dbContext.SaveChangesAsync(token);
        return Ok();
    }

    /// <summary>
    /// Force a sync now by setting <c>NextRunUtc = UtcNow</c>. The watcher
    /// will pick it up on the next 30-second tick.
    /// </summary>
    [RequireGameAdmin]
    [HttpPost("Games/{id:int}/Watches/{watchId:int}/Run")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RunRepoWatchNow(
        [FromRoute] int id, [FromRoute] int watchId, CancellationToken token)
    {
        var watch = await dbContext.RepoWatches.FirstOrDefaultAsync(
            w => w.Id == watchId && w.GameId == id, token);
        if (watch is null)
            return NotFound(new RequestResponse("Watch not found."));

        watch.NextRunUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(token);
        return Ok();
    }
}
