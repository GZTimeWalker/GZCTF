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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NSwag.Annotations;

namespace GZCTF.Controllers;

/// <summary>
/// Data Modification APIs
/// </summary>
[RequireAdmin]
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
    IStringLocalizer<Program> localizer) : Controller
{
    /// <summary>
    /// Add Post
    /// </summary>
    /// <remarks>
    /// Adding a post requires administrator privileges
    /// </remarks>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully added post</response>
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
    /// Retrieving the game list requires administrator privileges
    /// </remarks>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game list</response>
    [HttpGet("Games")]
    [ProducesResponseType(typeof(ArrayResponse<GameInfoModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGames([FromQuery][Range(0, 100)] int count, [FromQuery] int skip,
        CancellationToken token) =>
        Ok((await gameRepository.GetGames(count, skip, token))
            .Select(GameInfoModel.FromGame)
            .ToResponse(await gameRepository.CountAsync(token)));

    /// <summary>
    /// Get Game
    /// </summary>
    /// <remarks>
    /// Retrieving a game requires administrator privileges
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game</response>
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
    /// Delete All WriteUps
    /// </summary>
    /// <remarks>
    /// Deleting all WriteUps for a game requires administrator privileges
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully deleted game WriteUps</response>
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

        var res = await gameNoticeRepository.AddNotice(
            new()
            {
                Values = [model.Content],
                GameId = game.Id,
                Type = NoticeType.Normal,
                PublishTimeUtc = DateTimeOffset.UtcNow
            }, token);

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
    /// Add Game Challenge
    /// </summary>
    /// <remarks>
    /// Adding a game challenge requires administrator privileges
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully added game challenge</response>
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
    [HttpGet("Games/{id:int}/Challenges")]
    [ProducesResponseType(typeof(ChallengeInfoModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGameChallenges([FromRoute] int id, CancellationToken token) =>
        Ok((await challengeRepository.GetChallenges(id, token)).Select(ChallengeInfoModel.FromChallenge));

    /// <summary>
    /// Update AC Counter for Challenges
    /// </summary>
    /// <remarks>
    /// Updating the accepted count for all game challenges requires administrator privileges
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully updated accepted counts</response>
    [HttpPost("Games/{id:int}/Challenges/UpdateAccepted")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateGameChallengesAcceptedCount([FromRoute] int id, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        if (await challengeRepository.RecalculateAcceptedCount(game, token))
            return Ok();

        return BadRequest();
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
    [HttpGet("Games/{id:int}/Challenges/{cId:int}")]
    [ProducesResponseType(typeof(ChallengeEditDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGameChallenge([FromRoute] int id, [FromRoute] int cId, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var challenge = await challengeRepository.GetChallenge(id, cId, token);

        if (challenge is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        // Do not load flags for dynamic containers
        if (challenge.Type != ChallengeType.DynamicContainer)
            await challengeRepository.LoadFlags(challenge, token);

        return Ok(ChallengeEditDetailModel.FromChallenge(challenge));
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
    [HttpPut("Games/{id:int}/Challenges/{cId:int}")]
    [ProducesResponseType(typeof(ChallengeEditDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGameChallenge([FromRoute] int id, [FromRoute] int cId,
        [FromBody] ChallengeUpdateModel model, CancellationToken token)
    {
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

        if (model.IsEnabled == true)
        {
            // Will also update IsEnabled
            await challengeRepository.EnsureInstances(res, game, token);

            if (game.IsActive)
                await gameNoticeRepository.AddNotice(
                    new() { Game = game, Type = NoticeType.NewChallenge, Values = [res.Title] }, token);
        }
        else
        {
            if (!res.Type.IsContainer())
                await instanceRepository.DestroyAllInstances(res, token);

            await challengeRepository.SaveAsync(token);
        }

        if (game.IsActive && res.IsEnabled && hintUpdated)
            await gameNoticeRepository.AddNotice(
                new() { Game = game, Type = NoticeType.NewHint, Values = [res.Title] },
                token);

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
    [HttpPost("Games/{id:int}/Challenges/{cId:int}/Container")]
    [ProducesResponseType(typeof(ContainerInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTestContainer([FromRoute] int id, [FromRoute] int cId,
        CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var challenge = await challengeRepository.GetChallenge(id, cId, token);

        if (challenge is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        if (!challenge.Type.IsContainer())
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerCreationNotAllowed)]));

        if (challenge.ContainerImage is null || challenge.ContainerExposePort is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Container_ConfigError)]));

        var user = await userManager.GetUserAsync(User);

        var container = await containerService.CreateContainerAsync(
            new()
            {
                TeamId = "admin",
                UserId = user!.Id,
                ChallengeId = challenge.Id,
                Flag = challenge.Type.IsDynamic() ? challenge.GenerateTestFlag() : null,
                Image = challenge.ContainerImage,
                CPUCount = challenge.CPUCount ?? 1,
                MemoryLimit = challenge.MemoryLimit ?? 64,
                StorageLimit = challenge.StorageLimit ?? 256,
                ExposedPort = challenge.ContainerExposePort ??
                              throw new ArgumentException(localizer[nameof(Resources.Program.Container_InvalidPort)])
            }, token);

        if (container is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Container_CreationFailed)]));

        challenge.TestContainer = container;
        await challengeRepository.SaveAsync(token);

        logger.Log(
            StaticLocalizer[nameof(Resources.Program.Container_TestContainerCreated), container.ContainerId],
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
    [HttpDelete("Games/{id:int}/Challenges/{cId:int}/Container")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DestroyTestContainer([FromRoute] int id, [FromRoute] int cId,
        CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

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
    [HttpDelete("Games/{id:int}/Challenges/{cId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveGameChallenge([FromRoute] int id, [FromRoute] int cId,
        CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var res = await challengeRepository.GetChallenge(id, cId, token);

        if (res is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        await challengeRepository.RemoveChallenge(res, true, token);

        // Always flush scoreboard
        await cacheHelper.FlushScoreboardCache(game.Id, token);

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
    [HttpPost("Games/{id:int}/Challenges/{cId:int}/Attachment")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAttachment([FromRoute] int id, [FromRoute] int cId,
        [FromBody] AttachmentCreateModel model, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

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
    [HttpPost("Games/{id:int}/Challenges/{cId:int}/Flags")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddFlags([FromRoute] int id, [FromRoute] int cId,
        [FromBody] FlagCreateModel[] models, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

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
    [HttpDelete("Games/{id:int}/Challenges/{cId:int}/Flags/{fId:int}")]
    [ProducesResponseType(typeof(TaskStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFlag([FromRoute] int id, [FromRoute] int cId, [FromRoute] int fId,
        CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var challenge = await challengeRepository.GetChallenge(id, cId, token);

        if (challenge is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        return Ok(await challengeRepository.RemoveFlag(challenge, fId, token));
    }
}
