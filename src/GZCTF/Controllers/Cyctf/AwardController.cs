using GZCTF.Middlewares;
using GZCTF.Models.Request.Cyctf;
using GZCTF.Models.Response.Cyctf;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace GZCTF.Controllers.Cyctf;

/// <summary>
/// CYCTF 奖项 API
/// </summary>
[Route("api/cyctf/games/{gameId:int}/awards")]
[ApiController]
public class AwardController(
    IAwardRepository awardRepository,
    IGameRepository gameRepository,
    IStringLocalizer<Program> localizer) : ControllerBase
{
    /// <summary>
    /// 获取比赛所有奖项
    /// </summary>
    /// <param name="gameId">比赛 ID</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取奖项列表</response>
    /// <response code="404">比赛不存在</response>
    [HttpGet]
    [ProducesResponseType(typeof(AwardResponse[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAwards(int gameId, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(gameId, token);
        if (game is null)
            return NotFound(new RequestResponse("比赛不存在", StatusCodes.Status404NotFound));

        var awards = await awardRepository.GetAwardsByGameId(gameId, token);
        return Ok(awards.Select(AwardResponse.FromEntity));
    }

    /// <summary>
    /// 获取单个奖项
    /// </summary>
    /// <param name="gameId">比赛 ID</param>
    /// <param name="id">奖项 ID</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取奖项</response>
    /// <response code="404">奖项不存在</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AwardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAward(int gameId, int id, CancellationToken token)
    {
        var award = await awardRepository.GetAwardById(id, token);
        if (award is null || award.GameId != gameId)
            return NotFound(new RequestResponse("奖项不存在", StatusCodes.Status404NotFound));

        return Ok(AwardResponse.FromEntity(award));
    }

    /// <summary>
    /// 创建奖项
    /// </summary>
    /// <param name="gameId">比赛 ID</param>
    /// <param name="request">奖项信息</param>
    /// <param name="token"></param>
    /// <response code="200">成功创建奖项</response>
    /// <response code="404">比赛不存在</response>
    [HttpPost]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(AwardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAward(int gameId, [FromBody] AwardRequest request, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(gameId, token);
        if (game is null)
            return NotFound(new RequestResponse("比赛不存在", StatusCodes.Status404NotFound));

        var award = new Models.Data.Cyctf.Award
        {
            GameId = gameId,
            Name = request.Name,
            Description = request.Description,
            PrimaryColor = request.PrimaryColor,
            SecondaryColor = request.SecondaryColor,
            SortOrder = request.SortOrder
        };

        var result = await awardRepository.CreateAward(award, token);
        return Ok(AwardResponse.FromEntity(result));
    }

    /// <summary>
    /// 更新奖项
    /// </summary>
    /// <param name="gameId">比赛 ID</param>
    /// <param name="id">奖项 ID</param>
    /// <param name="request">奖项信息</param>
    /// <param name="token"></param>
    /// <response code="200">成功更新奖项</response>
    /// <response code="404">奖项不存在</response>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(AwardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAward(int gameId, int id, [FromBody] AwardRequest request, CancellationToken token)
    {
        var award = await awardRepository.GetAwardById(id, token);
        if (award is null || award.GameId != gameId)
            return NotFound(new RequestResponse("奖项不存在", StatusCodes.Status404NotFound));

        award.Name = request.Name;
        award.Description = request.Description;
        award.PrimaryColor = request.PrimaryColor;
        award.SecondaryColor = request.SecondaryColor;
        award.SortOrder = request.SortOrder;

        var result = await awardRepository.UpdateAward(award, token);
        return Ok(AwardResponse.FromEntity(result!));
    }

    /// <summary>
    /// 删除奖项
    /// </summary>
    /// <param name="gameId">比赛 ID</param>
    /// <param name="id">奖项 ID</param>
    /// <param name="token"></param>
    /// <response code="200">成功删除奖项</response>
    /// <response code="404">奖项不存在</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAward(int gameId, int id, CancellationToken token)
    {
        var award = await awardRepository.GetAwardById(id, token);
        if (award is null || award.GameId != gameId)
            return NotFound(new RequestResponse("奖项不存在", StatusCodes.Status404NotFound));

        var success = await awardRepository.DeleteAward(id, token);
        if (!success)
            return NotFound(new RequestResponse("奖项不存在", StatusCodes.Status404NotFound));

        return Ok(new RequestResponse("成功删除奖项", StatusCodes.Status200OK));
    }
}
