using GZCTF.Middlewares;
using GZCTF.Models.Request.Cyctf;
using GZCTF.Models.Response.Cyctf;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace GZCTF.Controllers.Cyctf;

/// <summary>
/// CYCTF 报名 API
/// </summary>
[Route("api/cyctf/registrations")]
[ApiController]
public class RegistrationController(
    IRegistrationRepository registrationRepository,
    IGameRepository gameRepository,
    IGameExtensionRepository gameExtensionRepository,
    IDivisionExtensionRepository divisionExtensionRepository,
    ITeamRepository teamRepository,
    IStringLocalizer<Program> localizer) : ControllerBase
{
    /// <summary>
    /// 队伍报名比赛
    /// </summary>
    /// <param name="request">报名信息</param>
    /// <param name="token"></param>
    /// <response code="200">报名成功</response>
    /// <response code="400">报名失败</response>
    /// <response code="404">比赛或队伍不存在</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegisterTeam([FromBody] RegistrationRequest request, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(request.GameId, token);
        if (game is null)
            return NotFound(new RequestResponse("比赛不存在", StatusCodes.Status404NotFound));

        var gameExtension = await gameExtensionRepository.GetGameExtensionByGameId(request.GameId, token);
        if (gameExtension is null)
            return NotFound(new RequestResponse("比赛未启用 CYCTF 扩展", StatusCodes.Status404NotFound));

        var now = DateTimeOffset.UtcNow;
        if (now < gameExtension.RegistrationStartTime)
            return BadRequest(new RequestResponse("报名尚未开始", StatusCodes.Status400BadRequest));

        if (now > gameExtension.RegistrationEndTime)
            return BadRequest(new RequestResponse("报名已结束", StatusCodes.Status400BadRequest));

        if (gameExtension.MaxTeams.HasValue && gameExtension.CurrentTeams >= gameExtension.MaxTeams.Value)
            return BadRequest(new RequestResponse("报名人数已满", StatusCodes.Status400BadRequest));

        var team = await teamRepository.GetTeamById(request.TeamId, token);
        if (team is null)
            return NotFound(new RequestResponse("队伍不存在", StatusCodes.Status404NotFound));

        var hasRegistration = await registrationRepository.HasRegistration(request.TeamId, request.GameId, token);
        if (hasRegistration)
            return BadRequest(new RequestResponse("该队伍已报名此比赛", StatusCodes.Status400BadRequest));

        var registration = new Models.Data.Cyctf.Registration
        {
            GameId = request.GameId,
            TeamId = request.TeamId,
            DivisionId = request.DivisionId,
            Status = "PENDING",
            FormData = request.FormData
        };

        var result = await registrationRepository.CreateRegistration(registration, token);
        await gameExtensionRepository.UpdateCurrentTeams(request.GameId, gameExtension.CurrentTeams + 1, token);

        return Ok(RegistrationResponse.FromEntity(result));
    }

    /// <summary>
    /// 获取比赛所有报名记录（管理员）
    /// </summary>
    /// <param name="gameId">比赛 ID</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取报名记录</response>
    /// <response code="404">比赛不存在</response>
    [HttpGet("games/{gameId:int}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(RegistrationResponse[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGameRegistrations(int gameId, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(gameId, token);
        if (game is null)
            return NotFound(new RequestResponse("比赛不存在", StatusCodes.Status404NotFound));

        var registrations = await registrationRepository.GetRegistrationsByGameId(gameId, token);
        return Ok(registrations.Select(RegistrationResponse.FromEntity));
    }

    /// <summary>
    /// 获取队伍的报名记录
    /// </summary>
    /// <param name="gameId">比赛 ID</param>
    /// <param name="teamId">队伍 ID</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取报名记录</response>
    /// <response code="404">报名记录不存在</response>
    [HttpGet("games/{gameId:int}/teams/{teamId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeamRegistration(int gameId, int teamId, CancellationToken token)
    {
        var registration = await registrationRepository.GetRegistrationByTeamAndGame(teamId, gameId, token);
        if (registration is null)
            return NotFound(new RequestResponse("报名记录不存在", StatusCodes.Status404NotFound));

        return Ok(RegistrationResponse.FromEntity(registration));
    }

    /// <summary>
    /// 审核报名（管理员）
    /// </summary>
    /// <param name="id">报名 ID</param>
    /// <param name="request">审核信息</param>
    /// <param name="token"></param>
    /// <response code="200">审核成功</response>
    /// <response code="404">报名记录不存在</response>
    [HttpPost("{id:int}/review")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReviewRegistration(int id, [FromBody] RegistrationReviewRequest request, CancellationToken token)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userGuid = Guid.TryParse(userId, out var guid) ? guid : (Guid?)null;

        var result = await registrationRepository.UpdateRegistrationStatus(
            id,
            request.Status,
            request.ReviewNote,
            userGuid,
            token);

        if (result is null)
            return NotFound(new RequestResponse("报名记录不存在", StatusCodes.Status404NotFound));

        return Ok(RegistrationResponse.FromEntity(result));
    }

    /// <summary>
    /// 获取比赛报名统计（管理员）
    /// </summary>
    /// <param name="gameId">比赛 ID</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取报名统计</response>
    /// <response code="404">比赛不存在</response>
    [HttpGet("games/{gameId:int}/stats")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(Dictionary<string, int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRegistrationStats(int gameId, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(gameId, token);
        if (game is null)
            return NotFound(new RequestResponse("比赛不存在", StatusCodes.Status404NotFound));

        var stats = await registrationRepository.GetRegistrationStats(gameId, token);
        return Ok(stats);
    }
}
