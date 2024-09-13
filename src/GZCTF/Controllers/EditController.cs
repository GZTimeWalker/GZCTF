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
/// 数据修改交互接口
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
    IFileRepository fileService,
    IStringLocalizer<Program> localizer) : Controller
{
    /// <summary>
    /// 添加文章
    /// </summary>
    /// <remarks>
    /// 添加文章，需要管理员权限
    /// </remarks>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">成功添加文章</response>
    [HttpPost("Posts")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddPost([FromBody] PostEditModel model, CancellationToken token)
    {
        UserInfo? user = await userManager.GetUserAsync(User);
        Post res = await postRepository.CreatePost(new Post().Update(model, user!), token);
        return Ok(res.Id);
    }

    /// <summary>
    /// 修改文章
    /// </summary>
    /// <remarks>
    /// 修改文章，需要管理员权限
    /// </remarks>
    /// <param name="id">文章Id</param>
    /// <param name="token"></param>
    /// <param name="model"></param>
    /// <response code="200">成功修改文章</response>
    /// <response code="404">未找到文章</response>
    [HttpPut("Posts/{id}")]
    [ProducesResponseType(typeof(PostDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePost(string id, [FromBody] PostEditModel model, CancellationToken token)
    {
        Post? post = await postRepository.GetPostById(id, token);

        if (post is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Post_NotFound)],
                StatusCodes.Status404NotFound));

        UserInfo? user = await userManager.GetUserAsync(User);

        await postRepository.UpdatePost(post.Update(model, user!), token);

        return Ok(PostDetailModel.FromPost(post));
    }

    /// <summary>
    /// 删除文章
    /// </summary>
    /// <remarks>
    /// 删除文章，需要管理员权限
    /// </remarks>
    /// <param name="id">文章Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功删除文章</response>
    /// <response code="404">未找到文章</response>
    [HttpDelete("Posts/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePost(string id, CancellationToken token)
    {
        Post? post = await postRepository.GetPostById(id, token);

        if (post is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Post_NotFound)],
                StatusCodes.Status404NotFound));

        await postRepository.RemovePost(post, token);

        return Ok();
    }

    /// <summary>
    /// 添加比赛
    /// </summary>
    /// <remarks>
    /// 添加比赛，需要管理员权限
    /// </remarks>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛</response>
    [HttpPost("Games")]
    [ProducesResponseType(typeof(GameInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddGame([FromBody] GameInfoModel model, CancellationToken token)
    {
        Game? game = await gameRepository.CreateGame(new Game().Update(model), token);

        if (game is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_CreationFailed)]));

        gameRepository.FlushGameInfoCache();

        return Ok(GameInfoModel.FromGame(game));
    }

    /// <summary>
    /// 获取比赛列表
    /// </summary>
    /// <remarks>
    /// 获取比赛列表，需要管理员权限
    /// </remarks>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛列表</response>
    [HttpGet("Games")]
    [ProducesResponseType(typeof(ArrayResponse<GameInfoModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGames([FromQuery] int count, [FromQuery] int skip, CancellationToken token) =>
        Ok((await gameRepository.GetGames(count, skip, token))
            .Select(GameInfoModel.FromGame)
            .ToResponse(await gameRepository.CountAsync(token)));

    /// <summary>
    /// 获取比赛
    /// </summary>
    /// <remarks>
    /// 获取比赛，需要管理员权限
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛</response>
    [HttpGet("Games/{id:int}")]
    [ProducesResponseType(typeof(GameInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGame([FromRoute] int id, CancellationToken token)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        return Ok(GameInfoModel.FromGame(game));
    }

    /// <summary>
    /// 获取比赛哈希盐
    /// </summary>
    /// <remarks>
    /// 获取比赛哈希盐，需要管理员权限
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛哈希加盐</response>
    [OpenApiIgnore]
    [HttpGet("Games/{id:int}/HashSalt")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHashSalt([FromRoute] int id, CancellationToken token)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        return Ok(game.TeamHashSalt);
    }

    /// <summary>
    /// 修改比赛
    /// </summary>
    /// <remarks>
    /// 修改比赛，需要管理员权限
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">成功修改比赛</response>
    [HttpPut("Games/{id:int}")]
    [ProducesResponseType(typeof(GameInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGame([FromRoute] int id, [FromBody] GameInfoModel model,
        CancellationToken token)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        game.Update(model);
        await gameRepository.SaveAsync(token);
        gameRepository.FlushGameInfoCache();
        await cacheHelper.FlushScoreboardCache(game.Id, token);

        return Ok(GameInfoModel.FromGame(game));
    }

    /// <summary>
    /// 删除比赛
    /// </summary>
    /// <remarks>
    /// 删除比赛，需要管理员权限
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <response code="200">成功删除比赛</response>
    [HttpDelete("Games/{id:int}")]
    [ProducesResponseType(typeof(GameInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGame([FromRoute] int id, CancellationToken token)
    {
        Game? game = await gameRepository.GetGameById(id, token);

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
    /// 删除比赛的全部 WriteUp
    /// </summary>
    /// <remarks>
    /// 删除比赛的全部 WriteUp，需要管理员权限
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <response code="200">成功删除比赛 WriteUps</response>
    [HttpDelete("Games/{id:int}/WriteUps")]
    [ProducesResponseType(typeof(GameInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGameWriteUps([FromRoute] int id, CancellationToken token)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        await gameRepository.DeleteAllWriteUps(game, token);

        return Ok();
    }

    /// <summary>
    /// 更新比赛头图
    /// </summary>
    /// <remarks>
    /// 使用此接口更新比赛头图，需要Admin权限
    /// </remarks>
    /// <response code="200">比赛头图URL</response>
    /// <response code="400">非法请求</response>
    /// <response code="401">未授权用户</response>
    [HttpPut("Games/{id:int}/Poster")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGamePoster([FromRoute] int id, IFormFile file, CancellationToken token)
    {
        if (file.Length == 0)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_SizeZero)]));

        if (file.Length > 3 * 1024 * 1024)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_SizeTooLarge)]));

        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        LocalFile? poster = await fileService.CreateOrUpdateImage(file, "poster", 0, token);

        if (poster is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_CreationFailed)]));

        game.PosterHash = poster.Hash;
        await gameRepository.SaveAsync(token);
        gameRepository.FlushGameInfoCache();

        return Ok(poster.Url());
    }

    /// <summary>
    /// 添加比赛通知
    /// </summary>
    /// <remarks>
    /// 添加比赛通知，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="model">通知内容</param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛通知</response>
    [HttpPost("Games/{id:int}/Notices")]
    [ProducesResponseType(typeof(GameNotice), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddGameNotice([FromRoute] int id, [FromBody] GameNoticeModel model,
        CancellationToken token)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        GameNotice res = await gameNoticeRepository.AddNotice(
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
    /// 获取比赛通知
    /// </summary>
    /// <remarks>
    /// 获取比赛通知，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛通知</response>
    [HttpGet("Games/{id:int}/Notices")]
    [ProducesResponseType(typeof(GameNotice[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGameNotices([FromRoute] int id, CancellationToken token = default)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        return Ok(await gameNoticeRepository.GetNormalNotices(id, token));
    }

    /// <summary>
    /// 更新比赛通知
    /// </summary>
    /// <remarks>
    /// 更新比赛通知，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="noticeId">通知Id</param>
    /// <param name="model">通知内容</param>
    /// <param name="token"></param>
    /// <response code="200">成功更新通知</response>
    [HttpPut("Games/{id:int}/Notices/{noticeId:int}")]
    [ProducesResponseType(typeof(GameNotice), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGameNotice([FromRoute] int id, [FromRoute] int noticeId,
        [FromBody] GameNoticeModel model, CancellationToken token = default)
    {
        GameNotice? notice = await gameNoticeRepository.GetNoticeById(id, noticeId, token);

        if (notice is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Notification_NotFound)],
                StatusCodes.Status404NotFound));

        if (notice.Type != NoticeType.Normal)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Notification_SystemNotEditable)]));

        notice.Values = [model.Content];
        return Ok(await gameNoticeRepository.UpdateNotice(notice, token));
    }

    /// <summary>
    /// 删除比赛通知
    /// </summary>
    /// <remarks>
    /// 删除比赛通知，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="noticeId">文章Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功删除文章</response>
    /// <response code="404">未找到文章</response>
    [HttpDelete("Games/{id:int}/Notices/{noticeId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGameNotice([FromRoute] int id, [FromRoute] int noticeId,
        CancellationToken token)
    {
        GameNotice? notice = await gameNoticeRepository.GetNoticeById(id, noticeId, token);

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
    /// 添加比赛题目
    /// </summary>
    /// <remarks>
    /// 添加比赛题目，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛题目</response>
    [HttpPost("Games/{id:int}/Challenges")]
    [ProducesResponseType(typeof(ChallengeEditDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddGameChallenge([FromRoute] int id, [FromBody] ChallengeInfoModel model,
        CancellationToken token)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        GameChallenge res = await challengeRepository.CreateChallenge(game,
            new GameChallenge { Title = model.Title, Type = model.Type, Category = model.Category }, token);

        return Ok(ChallengeEditDetailModel.FromChallenge(res));
    }

    /// <summary>
    /// 获取全部比赛题目
    /// </summary>
    /// <remarks>
    /// 获取全部比赛题目，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛题目</response>
    [HttpGet("Games/{id:int}/Challenges")]
    [ProducesResponseType(typeof(ChallengeInfoModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGameChallenges([FromRoute] int id, CancellationToken token) =>
        Ok((await challengeRepository.GetChallenges(id, token)).Select(ChallengeInfoModel.FromChallenge));

    /// <summary>
    /// 更新全部比赛题目解出数量
    /// </summary>
    /// <remarks>
    /// 更新全部比赛题目解出数量，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛题目</response>
    [HttpPost("Games/{id:int}/Challenges/UpdateAccepted")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateGameChallengesAcceptedCount([FromRoute] int id, CancellationToken token)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        if (await challengeRepository.RecalculateAcceptedCount(game, token))
            return Ok();

        return BadRequest();
    }


    /// <summary>
    /// 获取比赛题目
    /// </summary>
    /// <remarks>
    /// 获取比赛题目，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="cId">题目Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛题目</response>
    [HttpGet("Games/{id:int}/Challenges/{cId:int}")]
    [ProducesResponseType(typeof(ChallengeEditDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGameChallenge([FromRoute] int id, [FromRoute] int cId, CancellationToken token)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        GameChallenge? res = await challengeRepository.GetChallenge(id, cId, true, token);

        if (res is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        return Ok(ChallengeEditDetailModel.FromChallenge(res));
    }

    /// <summary>
    /// 修改比赛题目信息，Flags 不受更改，使用 Flag 相关 API 修改
    /// </summary>
    /// <remarks>
    /// 修改比赛题目，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="cId">题目Id</param>
    /// <param name="model">题目信息</param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛题目</response>
    [HttpPut("Games/{id:int}/Challenges/{cId:int}")]
    [ProducesResponseType(typeof(ChallengeEditDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGameChallenge([FromRoute] int id, [FromRoute] int cId,
        [FromBody] ChallengeUpdateModel model, CancellationToken token)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        GameChallenge? res = await challengeRepository.GetChallenge(id, cId, true, token);

        if (res is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        // NOTE: IsEnabled can only be updated outside the edit page
        if (model.IsEnabled == true && res.Flags.Count == 0 && res.Type != ChallengeType.DynamicContainer)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NoFlag)]));

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
            // will also update IsEnabled
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

        // always flush scoreboard
        await cacheHelper.FlushScoreboardCache(game.Id, token);

        return Ok(ChallengeEditDetailModel.FromChallenge(res));
    }

    /// <summary>
    /// 测试比赛题目容器
    /// </summary>
    /// <remarks>
    /// 测试比赛题目容器，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="cId">题目Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功开启比赛题目容器</response>
    [HttpPost("Games/{id:int}/Challenges/{cId:int}/Container")]
    [ProducesResponseType(typeof(ContainerInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTestContainer([FromRoute] int id, [FromRoute] int cId,
        CancellationToken token)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        GameChallenge? challenge = await challengeRepository.GetChallenge(id, cId, true, token);

        if (challenge is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        if (!challenge.Type.IsContainer())
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerCreationNotAllowed)]));

        if (challenge.ContainerImage is null || challenge.ContainerExposePort is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Container_ConfigError)]));

        UserInfo? user = await userManager.GetUserAsync(User);

        Container? container = await containerService.CreateContainerAsync(
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
            Program.StaticLocalizer[nameof(Resources.Program.Container_TestContainerCreated), container.ContainerId],
            user,
            TaskStatus.Success);

        return Ok(ContainerInfoModel.FromContainer(container));
    }

    /// <summary>
    /// 关闭测试比赛题目容器
    /// </summary>
    /// <remarks>
    /// 关闭测试比赛题目容器，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="cId">题目Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛题目</response>
    [HttpDelete("Games/{id:int}/Challenges/{cId:int}/Container")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DestroyTestContainer([FromRoute] int id, [FromRoute] int cId,
        CancellationToken token)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        GameChallenge? challenge = await challengeRepository.GetChallenge(id, cId, true, token);

        if (challenge is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        if (challenge.TestContainer is null)
            return Ok();

        await containerRepository.DestroyContainer(challenge.TestContainer, token);

        return Ok();
    }

    /// <summary>
    /// 删除比赛题目
    /// </summary>
    /// <remarks>
    /// 删除比赛题目，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="cId">题目Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛题目</response>
    [HttpDelete("Games/{id:int}/Challenges/{cId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveGameChallenge([FromRoute] int id, [FromRoute] int cId,
        CancellationToken token)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        GameChallenge? res = await challengeRepository.GetChallenge(id, cId, true, token);

        if (res is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        await challengeRepository.RemoveChallenge(res, token);

        // always flush scoreboard
        await cacheHelper.FlushScoreboardCache(game.Id, token);

        return Ok();
    }

    /// <summary>
    /// 更新比赛题目附件
    /// </summary>
    /// <remarks>
    /// 更新比赛题目附件，需要管理员权限，仅用于非动态附件题目
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="cId">题目Id</param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛题目数量</response>
    [HttpPost("Games/{id:int}/Challenges/{cId:int}/Attachment")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAttachment([FromRoute] int id, [FromRoute] int cId,
        [FromBody] AttachmentCreateModel model, CancellationToken token)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        GameChallenge? challenge = await challengeRepository.GetChallenge(id, cId, true, token);

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
    /// 添加比赛题目 Flag
    /// </summary>
    /// <remarks>
    /// 添加比赛题目 Flag，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="cId">题目Id</param>
    /// <param name="models"></param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛题目数量</response>
    [HttpPost("Games/{id:int}/Challenges/{cId:int}/Flags")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddFlags([FromRoute] int id, [FromRoute] int cId,
        [FromBody] FlagCreateModel[] models, CancellationToken token)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        GameChallenge? challenge = await challengeRepository.GetChallenge(id, cId, true, token);

        if (challenge is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        await challengeRepository.AddFlags(challenge, models, token);

        return Ok();
    }

    /// <summary>
    /// 删除比赛题目 Flag
    /// </summary>
    /// <remarks>
    /// 删除比赛题目 Flag，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="cId">题目Id</param>
    /// <param name="fId">Flag ID</param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛题目</response>
    [HttpDelete("Games/{id:int}/Challenges/{cId:int}/Flags/{fId:int}")]
    [ProducesResponseType(typeof(TaskStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFlag([FromRoute] int id, [FromRoute] int cId, [FromRoute] int fId,
        CancellationToken token)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        GameChallenge? challenge = await challengeRepository.GetChallenge(id, cId, true, token);

        if (challenge is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        return Ok(await challengeRepository.RemoveFlag(challenge, fId, token));
    }
}
