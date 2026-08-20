using GZCTF.Middlewares;
using GZCTF.Models.Request.Cyctf;
using GZCTF.Models.Response.Cyctf;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace GZCTF.Controllers.Cyctf;

/// <summary>
/// CYCTF 赞助商 API
/// </summary>
[Route("api/cyctf/games/{gameId:int}/sponsors")]
[ApiController]
public class SponsorController(
    ISponsorRepository sponsorRepository,
    IGameRepository gameRepository,
    IStringLocalizer<Program> localizer) : ControllerBase
{
    /// <summary>
    /// 获取比赛所有赞助商
    /// </summary>
    /// <param name="gameId">比赛 ID</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取赞助商列表</response>
    /// <response code="404">比赛不存在</response>
    [HttpGet]
    [ProducesResponseType(typeof(SponsorResponse[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSponsors(int gameId, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(gameId, token);
        if (game is null)
            return NotFound(new RequestResponse("比赛不存在", StatusCodes.Status404NotFound));

        var sponsors = await sponsorRepository.GetSponsorsByGameId(gameId, token);
        return Ok(sponsors.Select(SponsorResponse.FromEntity));
    }

    /// <summary>
    /// 获取单个赞助商
    /// </summary>
    /// <param name="gameId">比赛 ID</param>
    /// <param name="id">赞助商 ID</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取赞助商</response>
    /// <response code="404">赞助商不存在</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SponsorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSponsor(int gameId, int id, CancellationToken token)
    {
        var sponsor = await sponsorRepository.GetSponsorById(id, token);
        if (sponsor is null || sponsor.GameId != gameId)
            return NotFound(new RequestResponse("赞助商不存在", StatusCodes.Status404NotFound));

        return Ok(SponsorResponse.FromEntity(sponsor));
    }

    /// <summary>
    /// 创建赞助商
    /// </summary>
    /// <param name="gameId">比赛 ID</param>
    /// <param name="request">赞助商信息</param>
    /// <param name="token"></param>
    /// <response code="200">成功创建赞助商</response>
    /// <response code="404">比赛不存在</response>
    [HttpPost]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(SponsorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateSponsor(int gameId, [FromBody] SponsorRequest request, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(gameId, token);
        if (game is null)
            return NotFound(new RequestResponse("比赛不存在", StatusCodes.Status404NotFound));

        var sponsor = new Models.Data.Cyctf.Sponsor
        {
            GameId = gameId,
            ShortName = request.ShortName,
            FullName = request.FullName,
            Website = request.Website,
            LogoUrl = request.LogoUrl,
            Type = request.Type,
            TypeLabel = request.TypeLabel,
            SortOrder = request.SortOrder
        };

        var result = await sponsorRepository.CreateSponsor(sponsor, token);
        return Ok(SponsorResponse.FromEntity(result));
    }

    /// <summary>
    /// 更新赞助商
    /// </summary>
    /// <param name="gameId">比赛 ID</param>
    /// <param name="id">赞助商 ID</param>
    /// <param name="request">赞助商信息</param>
    /// <param name="token"></param>
    /// <response code="200">成功更新赞助商</response>
    /// <response code="404">赞助商不存在</response>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(SponsorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSponsor(int gameId, int id, [FromBody] SponsorRequest request, CancellationToken token)
    {
        var sponsor = await sponsorRepository.GetSponsorById(id, token);
        if (sponsor is null || sponsor.GameId != gameId)
            return NotFound(new RequestResponse("赞助商不存在", StatusCodes.Status404NotFound));

        sponsor.ShortName = request.ShortName;
        sponsor.FullName = request.FullName;
        sponsor.Website = request.Website;
        sponsor.LogoUrl = request.LogoUrl;
        sponsor.Type = request.Type;
        sponsor.TypeLabel = request.TypeLabel;
        sponsor.SortOrder = request.SortOrder;

        var result = await sponsorRepository.UpdateSponsor(sponsor, token);
        return Ok(SponsorResponse.FromEntity(result!));
    }

    /// <summary>
    /// 删除赞助商
    /// </summary>
    /// <param name="gameId">比赛 ID</param>
    /// <param name="id">赞助商 ID</param>
    /// <param name="token"></param>
    /// <response code="200">成功删除赞助商</response>
    /// <response code="404">赞助商不存在</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSponsor(int gameId, int id, CancellationToken token)
    {
        var sponsor = await sponsorRepository.GetSponsorById(id, token);
        if (sponsor is null || sponsor.GameId != gameId)
            return NotFound(new RequestResponse("赞助商不存在", StatusCodes.Status404NotFound));

        var success = await sponsorRepository.DeleteSponsor(id, token);
        if (!success)
            return NotFound(new RequestResponse("赞助商不存在", StatusCodes.Status404NotFound));

        return Ok(new RequestResponse("成功删除赞助商", StatusCodes.Status200OK));
    }
}
