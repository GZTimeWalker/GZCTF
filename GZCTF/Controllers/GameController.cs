using CTFServer.Models.Request.Game;
using CTFServer.Repositories.Interface;
using CTFServer.Utils;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System.Net.Mime;
using Microsoft.EntityFrameworkCore;
using CTFServer.Middlewares;
using CTFServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace CTFServer.Controllers;

/// <summary>
/// 比赛数据交互接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class GameController : ControllerBase
{
    private static readonly Logger logger = LogManager.GetLogger("GameController");
    
    private readonly IMemoryCache cache;
    private readonly UserManager<UserInfo> userManager;
    private readonly IGameRepository gameRepository;
    private readonly ITeamRepository teamRepository;
    private readonly ISubmissionRepository submissionRepository;
    private readonly IParticipationRepository participationRepository;
    
    public GameController(
        IMemoryCache memoryCache,
        UserManager<UserInfo> _userManager,
        IGameRepository _gameRepository,
        ITeamRepository _teamRepository,
        ISubmissionRepository _submissionRepository,
        IParticipationRepository _participationRepository)
    {
        cache = memoryCache;
        userManager = _userManager;
        gameRepository = _gameRepository;
        teamRepository = _teamRepository;
        submissionRepository = _submissionRepository;
        participationRepository = _participationRepository;
    }

    /// <summary>
    /// 获取最新的比赛
    /// </summary>
    /// <remarks>
    /// 获取最近十个比赛
    /// </remarks>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛信息</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<BasicGameInfoModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Games(CancellationToken token)
         => Ok((from game in await gameRepository.GetGames(10, 0, token)
                select BasicGameInfoModel.FromGame(game)).ToList());

    /// <summary>
    /// 获取比赛详细信息
    /// </summary>
    /// <remarks>
    /// 获取比赛的详细信息
    /// </remarks>
    /// <param name="id">比赛id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛信息</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GameDetailsModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Games(int id, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到"));

        return Ok(GameDetailsModel.FromGame(game));
    }

    /// <summary>
    /// 加入一个比赛
    /// </summary>
    /// <remarks>
    /// 加入一场比赛，需要User权限，需要当前激活队伍的队长权限
    /// </remarks>
    /// <param name="id">比赛id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛信息</response>
    [RequireUser]
    [HttpPost("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> JoinGame(int id, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到"));

        var user = await userManager.GetUserAsync(User);

        if (user.OwnTeamId != id || user.ActiveTeamId != id)
            return new JsonResult(new RequestResponse("您不是当前激活队伍的队长", 403))
            {
                StatusCode = 403
            };

        var team = await teamRepository.GetActiveTeamWithMembers(user, token);

        if (team is null)
            return BadRequest(new RequestResponse("请激活一个队伍以参赛"));

        if(game.TeamMemberLimitCount > 0 && game.TeamMemberLimitCount > team.Members.Count)
            return BadRequest(new RequestResponse("参赛队伍不符合人数限制"));

        if (game.StartTimeUTC < DateTimeOffset.UtcNow)
            return BadRequest(new RequestResponse("比赛已经开始，不能加入"));

        await participationRepository.CreateParticipation(team, game, token);
        team.Locked = true;
        await teamRepository.UpdateAsync(team, token);

        LogHelper.Log(logger, $"{team.Name} 报名了比赛 {game.Title}", user, TaskStatus.Success);

        return Ok();
    }

    [HttpGet("{id}/Scoreboard")]
    [ProducesResponseType(typeof(Scoreboard), StatusCodes.Status200OK)]
    public async Task<IActionResult> Scoreboard([FromRoute] int id, CancellationToken token)
    {
        var result = await cache.GetOrCreateAsync(CacheKey.ScoreBoard,
            entry => gameRepository.FlushScoreboard(id, token));
        return Ok(result);
    }
}        
