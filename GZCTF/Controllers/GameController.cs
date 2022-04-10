using CTFServer.Models.Request.Game;
using CTFServer.Repositories.Interface;
using CTFServer.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using CTFServer.Middlewares;
using CTFServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using CTFServer.Repositories;
using System.Threading.Channels;

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
    private readonly ILogger<GameController> logger;
    private readonly UserManager<UserInfo> userManager;
    private readonly ChannelWriter<Submission> channelWriter;
    private readonly IGameRepository gameRepository;
    private readonly ITeamRepository teamRepository;
    private readonly IGameNoticeRepository noticeRepository;
    private readonly IGameEventRepository eventRepository;
    private readonly IInstanceRepository instanceRepository;
    private readonly ISubmissionRepository submissionRepository;
    private readonly IParticipationRepository participationRepository;

    public GameController(
        ILogger<GameController> _logger,
        UserManager<UserInfo> _userManager,
        ChannelWriter<Submission> _channelWriter,
        IGameRepository _gameRepository,
        ITeamRepository _teamRepository,
        IGameNoticeRepository _noticeRepository,
        IGameEventRepository _eventRepository,
        IInstanceRepository _instanceRepository,
        ISubmissionRepository _submissionRepository,
        IParticipationRepository _participationRepository)
    {
        logger = _logger;
        userManager = _userManager;
        channelWriter = _channelWriter;
        gameRepository = _gameRepository;
        teamRepository = _teamRepository;
        eventRepository = _eventRepository;
        noticeRepository = _noticeRepository;
        instanceRepository = _instanceRepository;
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

        using var trans = await teamRepository.BeginTransactionAsync(token);

        try
        {
            var team = await teamRepository.GetActiveTeamWithMembers(user, token);

            if (team is null)
                return BadRequest(new RequestResponse("请激活一个队伍以参赛"));

            if (game.TeamMemberLimitCount > 0 && game.TeamMemberLimitCount > team.Members.Count)
                return BadRequest(new RequestResponse("参赛队伍不符合人数限制"));

            if (game.StartTimeUTC < DateTimeOffset.UtcNow)
                return BadRequest(new RequestResponse("比赛已经开始，不能加入"));

            await participationRepository.CreateParticipation(team, game, token);

            team.Locked = true;

            await teamRepository.UpdateAsync(team, token);

            await trans.CommitAsync(token);

            logger.Log($"{team.Name} 报名了比赛 {game.Title}", user, TaskStatus.Success);

            return Ok();
        }
        catch
        {
            await trans.RollbackAsync(token);
            throw;
        }
    }

    /// <summary>
    /// 获取积分榜
    /// </summary>
    /// <remarks>
    /// 获取积分榜数据
    /// </remarks>
    /// <param name="id">比赛id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛信息</response>
    /// <response code="400">比赛未找到</response>
    [HttpGet("{id}/Scoreboard")]
    [ProducesResponseType(typeof(ScoreboardModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Scoreboard([FromRoute] int id, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到"));

        if (DateTimeOffset.UtcNow < game.StartTimeUTC)
            return BadRequest(new RequestResponse("比赛还未开始"));

        return Ok(await gameRepository.GetScoreboard(game, token));
    }

    /// <summary>
    /// 获取比赛事件
    /// </summary>
    /// <remarks>
    /// 获取比赛事件数据
    /// </remarks>
    /// <param name="id">比赛id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛事件</response>
    /// <response code="400">比赛未找到</response>
    [HttpGet("{id}/Notices")]
    [ProducesResponseType(typeof(GameEvent[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Notices([FromRoute] int id, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到"));

        if (DateTimeOffset.UtcNow < game.StartTimeUTC)
            return BadRequest(new RequestResponse("比赛还未开始"));

        return Ok(await noticeRepository.GetNotices(game, 30, 0, token));
    }

    /// <summary>
    /// 获取比赛提交
    /// </summary>
    /// <remarks>
    /// 获取比赛提交数据
    /// </remarks>
    /// <param name="id">比赛id</param>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛提交</response>
    /// <response code="400">比赛未找到</response>
    [RequireMonitor]
    [HttpGet("{id}/Submissions")]
    [ProducesResponseType(typeof(Submission[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submissions([FromRoute] int id, [FromQuery] int count = 100, [FromQuery] int skip = 0, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到"));

        if (DateTimeOffset.UtcNow < game.StartTimeUTC)
            return BadRequest(new RequestResponse("比赛还未开始"));

        return Ok(await submissionRepository.GetSubmissions(game, count, skip, token));
    }

    /// <summary>
    /// 获取比赛提交
    /// </summary>
    /// <remarks>
    /// 获取比赛提交数据
    /// </remarks>
    /// <param name="id">比赛id</param>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛提交</response>
    /// <response code="400">比赛未找到</response>
    [RequireMonitor]
    [HttpGet("{id}/Instances")]
    [ProducesResponseType(typeof(InstanceInfoModel[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Instances([FromRoute] int id, [FromQuery] int count = 100, [FromQuery] int skip = 0, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到"));

        if (DateTimeOffset.UtcNow < game.StartTimeUTC)
            return BadRequest(new RequestResponse("比赛还未开始"));

        return Ok((await instanceRepository.GetInstances(game, count, skip, token))
            .Select(i => InstanceInfoModel.FromInstance(i)));
    }

    /// <summary>
    /// 获取全部比赛题目信息
    /// </summary>
    /// <remarks>
    /// 获取比赛的全部题目，需要User权限，需要当前激活队伍已经报名
    /// </remarks>
    /// <param name="id">比赛id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛题目信息</response>
    [RequireUser]
    [HttpGet("{id}/Challenges")]
    [ProducesResponseType(typeof(IDictionary<string, IEnumerable<ChallengeInfo>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Challenges([FromRoute] int id, CancellationToken token)
    {
        var context = await GetContextInfo(id, true, token);

        if (context.Result is not null)
            return context.Result;

        // get from cache
        return Ok((await gameRepository.GetScoreboard(context.Game!, token)).Challenges);
    }

    /// <summary>
    /// 获取比赛题目信息
    /// </summary>
    /// <remarks>
    /// 获取比赛题目信息，需要User权限，需要当前激活队伍已经报名
    /// </remarks>
    /// <param name="id">比赛id</param>
    /// <param name="challengeId">题目id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛题目信息</response>
    [RequireUser]
    [HttpGet("{id}/Challenges/{challengeId}")]
    [ProducesResponseType(typeof(ChallengeDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChallenge([FromRoute] int id, [FromRoute] int challengeId, CancellationToken token)
    {
        var context = await GetContextInfo(id, true, token);

        if (context.Result is not null)
            return context.Result;

        var instance = await instanceRepository.GetInstance(context.Participation!, challengeId, token);
        
        if(instance is null)
            return NotFound(new RequestResponse("题目未找到",404));

        return Ok(ChallengeDetailModel.FromInstance(instance));
    }

    /// <summary>
    /// 提交 flag
    /// </summary>
    /// <remarks>
    /// 提交flag，需要User权限，需要当前激活队伍已经报名
    /// </remarks>
    /// <param name="id">比赛id</param>
    /// <param name="challengeId">题目id</param>
    /// <param name="flag">提交Flag</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛题目信息</response>
    [RequireUser]
    [HttpGet("{id}/Submit/{challengeId}")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> Submit([FromRoute] int id, [FromRoute] int challengeId, [FromBody] string flag, CancellationToken token)
    {
        var context = await GetContextInfo(id, true, token);

        if (context.Result is not null)
            return context.Result;

        Submission submission = new()
        {
            ChallengeId = challengeId,
            Answer = flag,
            Game = context.Game!,
            User = context.User,
            Participation = context.Participation!,
            Status = AnswerResult.Submitted,
            SubmitTimeUTC = DateTimeOffset.UtcNow,
        };

        submission = await submissionRepository.AddSubmission(submission, token);
        
        // send to flag checker service
        await channelWriter.WriteAsync(submission, token);

        return Ok(submission.Id);
    }

    private class ContextInfo
    {
        public Game? Game = null;
        public UserInfo? User = null;
        public Participation? Participation = null;
        public IActionResult? Result = null;

        public ContextInfo WithResult(IActionResult res)
        {
            Result = res;
            return this;
        }
    };

    private async Task<ContextInfo> GetContextInfo(int id, bool withPartication = false, CancellationToken token = default)
    {
        ContextInfo res = new();

        res.User = await userManager.GetUserAsync(User);

        res.Game = await gameRepository.GetGameById(id, token);

        if (res.Game is null)
            return res.WithResult(NotFound(new RequestResponse("比赛未找到")));

        if (DateTimeOffset.UtcNow < res.Game.StartTimeUTC)
            return res.WithResult(BadRequest(new RequestResponse("比赛还未开始")));

        if (!withPartication)
            return res;

        if (res.User!.ActiveTeam is null)
            return res.WithResult(BadRequest(new RequestResponse("请激活一个队伍以参赛")));

        res.Participation = await participationRepository.GetParticipation(res.User.ActiveTeam, res.Game, token);

        if (res.Participation is null)
            return res.WithResult(BadRequest(new RequestResponse("您尚未参赛")));

        return res;
    }
}
