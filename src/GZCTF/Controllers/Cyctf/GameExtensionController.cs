using GZCTF.Middlewares;
using GZCTF.Models.Request.Cyctf;
using GZCTF.Models.Response.Cyctf;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace GZCTF.Controllers.Cyctf;

/// <summary>
/// CYCTF 比赛扩展 API
/// </summary>
[Route("api/cyctf/games/{gameId:int}/extension")]
[ApiController]
[Authorize(Policy = "Admin")]
public class GameExtensionController(
    IGameExtensionRepository gameExtensionRepository,
    IGameRepository gameRepository,
    IStringLocalizer<Program> localizer) : ControllerBase
{
    /// <summary>
    /// 获取比赛扩展信息
    /// </summary>
    /// <param name="gameId">比赛 ID</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛扩展信息</response>
    /// <response code="404">比赛或扩展信息不存在</response>
    [HttpGet]
    [ProducesResponseType(typeof(GameExtensionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGameExtension(int gameId, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(gameId, token);
        if (game is null)
            return NotFound(new RequestResponse("比赛不存在", StatusCodes.Status404NotFound));

        var extension = await gameExtensionRepository.GetGameExtensionByGameId(gameId, token);
        if (extension is null)
            return NotFound(new RequestResponse("比赛扩展信息不存在", StatusCodes.Status404NotFound));

        return Ok(GameExtensionResponse.FromEntity(extension));
    }

    /// <summary>
    /// 创建或更新比赛扩展信息
    /// </summary>
    /// <param name="gameId">比赛 ID</param>
    /// <param name="request">扩展信息请求</param>
    /// <param name="token"></param>
    /// <response code="200">成功创建或更新比赛扩展信息</response>
    /// <response code="404">比赛不存在</response>
    [HttpPut]
    [ProducesResponseType(typeof(GameExtensionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateOrUpdateGameExtension(int gameId, [FromBody] GameExtensionRequest request, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(gameId, token);
        if (game is null)
            return NotFound(new RequestResponse("比赛不存在", StatusCodes.Status404NotFound));

        var extension = new Models.Data.Cyctf.GameExtension
        {
            GameId = gameId,
            RegistrationStartTime = request.RegistrationStartTime,
            RegistrationEndTime = request.RegistrationEndTime,
            MaxTeams = request.MaxTeams,
            ShowRegistrationCount = request.ShowRegistrationCount,
            ShowEventTime = request.ShowEventTime,
            EmailWhitelist = request.EmailWhitelist,
            Status = request.Status
        };

        var result = await gameExtensionRepository.CreateOrUpdateGameExtension(extension, token);
        return Ok(GameExtensionResponse.FromEntity(result));
    }

    /// <summary>
    /// 删除比赛扩展信息
    /// </summary>
    /// <param name="gameId">比赛 ID</param>
    /// <param name="token"></param>
    /// <response code="200">成功删除比赛扩展信息</response>
    /// <response code="404">比赛或扩展信息不存在</response>
    [HttpDelete]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGameExtension(int gameId, CancellationToken token)
    {
        var success = await gameExtensionRepository.DeleteGameExtension(gameId, token);
        if (!success)
            return NotFound(new RequestResponse("比赛扩展信息不存在", StatusCodes.Status404NotFound));

        return Ok(new RequestResponse("成功删除比赛扩展信息", StatusCodes.Status200OK));
    }
}
