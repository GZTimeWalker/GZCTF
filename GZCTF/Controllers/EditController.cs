using CTFServer.Middlewares;
using CTFServer.Models.Request.Edit;
using CTFServer.Repositories.Interface;
using CTFServer.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace CTFServer.Controllers;

/// <summary>
/// 数据修改交互接口
/// </summary>
[RequireAdmin]
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class EditController : Controller
{
    private readonly INoticeRepository noticeRepository;
    private readonly IGameNoticeRepository gameNoticeRepository;
    private readonly IGameRepository gameRepository;
    private readonly IChallengeRepository challengeRepository;

    public EditController(INoticeRepository _noticeRepository,
        IChallengeRepository _challengeRepository,
        IGameNoticeRepository _gameNoticeRepository,
        IGameRepository _gameRepository)
    {
        noticeRepository = _noticeRepository;
        gameNoticeRepository = _gameNoticeRepository;
        gameRepository = _gameRepository;
        challengeRepository = _challengeRepository;
    }

    /// <summary>
    /// 添加公告
    /// </summary>
    /// <remarks>
    /// 添加公告，需要管理员权限
    /// </remarks>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">成功添加公告</response>
    [HttpPost("Notices")]
    [ProducesResponseType(typeof(Notice), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddNotice([FromBody] NoticeModel model, CancellationToken token)
    {
        var res = await noticeRepository.CreateNotice(new()
        {
            Content = model.Content,
            Title = model.Title,
            IsPinned = model.IsPinned,
            PublishTimeUTC = DateTimeOffset.UtcNow
        }, token);
        return Ok(res);
    }

    /// <summary>
    /// 获取所有公告
    /// </summary>
    /// <remarks>
    /// 获取所有公告，需要管理员权限
    /// </remarks>
    /// <param name="token"></param>
    /// <response code="200">成功获取文件</response>
    [HttpGet("Notices")]
    [ProducesResponseType(typeof(Notice[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotices(CancellationToken token)
        => Ok(await noticeRepository.GetNotices(token));

    /// <summary>
    /// 修改公告
    /// </summary>
    /// <remarks>
    /// 修改公告，需要管理员权限
    /// </remarks>
    /// <param name="id">公告Id</param>
    /// <param name="token"></param>
    /// <param name="model"></param>
    /// <response code="200">成功修改公告</response>
    /// <response code="404">未找到公告</response>
    [HttpPut("Notices/{id}")]
    [ProducesResponseType(typeof(Notice), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNotice(int id, [FromBody] NoticeModel model, CancellationToken token)
    {
        var notice = await noticeRepository.GetNoticeById(id, token);

        if (notice is null)
            return NotFound(new RequestResponse("公告未找到", 404));

        notice.UpdateInfo(model);
        await noticeRepository.UpdateAsync(notice, token);

        return Ok(notice);
    }

    /// <summary>
    /// 删除公告
    /// </summary>
    /// <remarks>
    /// 删除公告，需要管理员权限
    /// </remarks>
    /// <param name="id">公告Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功删除公告</response>
    /// <response code="404">未找到公告</response>
    [HttpDelete("Notices/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNotice(int id, CancellationToken token)
    {
        var notice = await noticeRepository.GetNoticeById(id, token);

        if (notice is null)
            return NotFound(new RequestResponse("公告未找到", 404));

        await noticeRepository.RemoveNotice(notice, token);

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
    /// <response code="200">成功获取文件</response>
    [HttpPost("Games")]
    [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddGame([FromBody] GameInfoModel model, CancellationToken token)
    {
        var game = await gameRepository.CreateGame(new Game().Update(model), token);

        return Ok(game);
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
    /// <response code="200">成功获取文件</response>
    [HttpGet("Games")]
    [ProducesResponseType(typeof(GameInfoModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGames([FromQuery] int count, [FromQuery] int skip, CancellationToken token)
        => Ok((await gameRepository.GetGames(count, skip, token)).Select(g => GameInfoModel.FromGame(g)));

    /// <summary>
    /// 获取比赛
    /// </summary>
    /// <remarks>
    /// 获取比赛，需要管理员权限
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <response code="200">成功获取文件</response>
    [HttpGet("Games/{id}")]
    [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGame([FromRoute] int id, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        return Ok(game);
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
    /// <response code="200">成功获取文件</response>
    [HttpPut("Games/{id}")]
    [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGame([FromRoute] int id, [FromBody] GameInfoModel model, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        game.Update(model);

        return Ok(game);
    }

    /// <summary>
    /// 添加比赛公告
    /// </summary>
    /// <remarks>
    /// 添加比赛公告，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛ID</param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛公告</response>
    [HttpPost("Games/{id}/Notices")]
    [ProducesResponseType(typeof(GameNotice), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddGameNotice([FromRoute] int id, [FromBody] GameNoticeModel model, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        var res = await gameNoticeRepository.CreateNotice(game, new()
        {
            Content = model.Content,
            Type = NoticeType.Normal,
            PublishTimeUTC = DateTimeOffset.UtcNow
        }, token);

        return Ok(res);
    }

    /// <summary>
    /// 获取比赛公告
    /// </summary>
    /// <remarks>
    /// 获取比赛公告，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛ID</param>
    /// <param name="count">数量</param>
    /// <param name="skip">跳过数量</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取文件</response>
    [HttpGet("Games/{id}/Notices")]
    [ProducesResponseType(typeof(GameNotice), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGameNotices([FromRoute] int id, [FromQuery] int count, [FromQuery] int skip, CancellationToken token)
        => Ok(await gameNoticeRepository.GetNotices(id, count, skip, token));

    /// <summary>
    /// 删除比赛公告
    /// </summary>
    /// <remarks>
    /// 删除比赛公告，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛ID</param>
    /// <param name="noticeId">公告Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功删除公告</response>
    /// <response code="404">未找到公告</response>
    [HttpDelete("Games/{id}/Notices/{noticeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGameNotice([FromRoute] int id, [FromRoute] int noticeId, CancellationToken token)
    {
        var notice = await gameNoticeRepository.GetNoticeById(id, noticeId, token);

        if (notice is null)
            return NotFound(new RequestResponse("公告未找到", 404));

        await gameNoticeRepository.RemoveNotice(notice, token);

        return Ok();
    }

    /// <summary>
    /// 添加比赛题目
    /// </summary>
    /// <remarks>
    /// 添加比赛题目，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛ID</param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛题目</response>
    [HttpPost("Games/{id}/Challenges")]
    [ProducesResponseType(typeof(Challenge), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddGameChallenge([FromRoute] int id, [FromBody] ChallengeModel model, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        var res = await challengeRepository.CreateChallenge(game, new Challenge().Update(model), token);

        return Ok(res);
    }

    /// <summary>
    /// 获取全部比赛题目
    /// </summary>
    /// <remarks>
    /// 获取全部比赛题目，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛ID</param>
    /// <param name="count">数量</param>
    /// <param name="skip">跳过数量</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛题目</response>
    [HttpGet("Games/{id}/Challenges")]
    [ProducesResponseType(typeof(ChallengeInfoModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGameChallenges([FromRoute] int id, [FromQuery] int count, [FromQuery] int skip, CancellationToken token)
        => Ok((await challengeRepository.GetChallenges(id, count, skip, token)).Select(c => ChallengeInfoModel.FromChallenge(c)));

    /// <summary>
    /// 获取比赛题目
    /// </summary>
    /// <remarks>
    /// 获取比赛题目，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛ID</param>
    /// <param name="cId">题目Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛题目</response>
    [HttpPost("Games/{id}/Challenges/{cId}")]
    [ProducesResponseType(typeof(Challenge), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddGameChallenge([FromRoute] int id, [FromRoute] int cId, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        var res = await challengeRepository.GetChallenge(id, cId, token);

        if (res is null)
            return NotFound(new RequestResponse("题目未找到", 404));

        return Ok(res);
    }

    /// <summary>
    /// 修改比赛题目信息
    /// </summary>
    /// <remarks>
    /// 修改比赛题目，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛ID</param>
    /// <param name="cId">题目Id</param>
    /// <param name="model">题目信息</param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛题目</response>
    [HttpPut("Games/{id}/Challenges/{cId}")]
    [ProducesResponseType(typeof(Challenge), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGameChallenge([FromRoute] int id, [FromRoute] int cId, [FromBody] ChallengeModel model, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        var res = await challengeRepository.GetChallenge(id, cId, token);

        if (res is null)
            return NotFound(new RequestResponse("题目未找到", 404));

        res.Update(model);
        await challengeRepository.UpdateAsync(res, token);

        return Ok(res);
    }

    /// <summary>
    /// 删除比赛题目
    /// </summary>
    /// <remarks>
    /// 删除比赛题目，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛ID</param>
    /// <param name="cId">题目Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛题目</response>
    [HttpDelete("Games/{id}/Challenges/{cId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveGameChallenge([FromRoute] int id, [FromRoute] int cId, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        var res = await challengeRepository.GetChallenge(id, cId, token);

        if (res is null)
            return NotFound(new RequestResponse("题目未找到", 404));

        await challengeRepository.RemoveChallenge(res, token);

        return Ok();
    }

    /// <summary>
    /// 添加比赛题目 Flag
    /// </summary>
    /// <remarks>
    /// 添加比赛题目 Flag，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛ID</param>
    /// <param name="cId">题目ID</param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛题目</response>
    [HttpPost("Games/{id}/Challenges/{cId}/Flags")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddFlag([FromRoute] int id, [FromRoute] int cId, [FromBody] FlagInfoModel model, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        var challenge = await challengeRepository.GetChallenge(id, cId, token);

        if (challenge is null)
            return NotFound(new RequestResponse("题目未找到", 404));

        var fid = await challengeRepository.AddFlag(challenge, model, token);

        return Ok(fid);
    }

    /// <summary>
    /// 删除比赛题目 Flag
    /// </summary>
    /// <remarks>
    /// 删除比赛题目 Flag，需要管理员权限
    /// </remarks>
    /// <param name="id">比赛ID</param>
    /// <param name="cId">题目ID</param>
    /// <param name="fId">Flag ID</param>
    /// <param name="token"></param>
    /// <response code="200">成功添加比赛题目</response>
    [HttpDelete("Games/{id}/Challenges/{cId}/Flags/{fId}")]
    [ProducesResponseType(typeof(TaskStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFlag([FromRoute] int id, [FromRoute] int cId, [FromRoute] int fId, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        var challenge = await challengeRepository.GetChallenge(id, cId, token);

        if (challenge is null)
            return NotFound(new RequestResponse("题目未找到", 404));

        return Ok(await challengeRepository.RemoveFlag(challenge, fId, token));
    }
}