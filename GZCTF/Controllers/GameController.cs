using CTFServer.Middlewares;
using CTFServer.Models.Request.Admin;
using CTFServer.Models.Request.Game;
using CTFServer.Repositories.Interface;
using CTFServer.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Security.Claims;
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
    private readonly ChannelWriter<Submission> checkerChannelWriter;
    private readonly IGameRepository gameRepository;
    private readonly ITeamRepository teamRepository;
    private readonly IContainerRepository containerRepository;
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
        IContainerRepository _containerRepository,
        IParticipationRepository _participationRepository)
    {
        logger = _logger;
        userManager = _userManager;
        checkerChannelWriter = _channelWriter;
        gameRepository = _gameRepository;
        teamRepository = _teamRepository;
        eventRepository = _eventRepository;
        noticeRepository = _noticeRepository;
        instanceRepository = _instanceRepository;
        containerRepository = _containerRepository;
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
    [ProducesResponseType(typeof(BasicGameInfoModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> Games(CancellationToken token)
         => Ok(await gameRepository.GetBasicGameInfo(10, 0, token));

    /// <summary>
    /// 获取比赛详细信息
    /// </summary>
    /// <remarks>
    /// 获取比赛的详细信息
    /// </remarks>
    /// <param name="id">比赛id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛信息</response>
    /// <response code="404">比赛未找到</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GameDetailsModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Games(int id, CancellationToken token)
    {
        var context = await GetContextInfo(id, token);

        if (context.Game is null)
            return NotFound(new RequestResponse("比赛未找到"));

        return Ok(GameDetailsModel.FromGame(context.Game)
                      .WithParticipation(context.Participation));
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
    /// <response code="403">无权操作或操作无效</response>
    /// <response code="404">比赛未找到</response>
    [RequireUser]
    [HttpPost("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> JoinGame(int id, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到"));

        var user = await userManager.GetUserAsync(User);

        if (user.OwnTeamId is null || user.OwnTeamId != user.ActiveTeamId)
            // (user.ActiveTeamId is null) is impossible
            return new JsonResult(new RequestResponse("您不是当前激活队伍的队长", 403))
            {
                StatusCode = 403
            };

        var team = await teamRepository.GetActiveTeamWithMembers(user, token);

        await participationRepository.CreateParticipation(team!, game, token);

        logger.Log($"{team!.Name} 报名了比赛 {game.Title}", user, TaskStatus.Success);

        return Ok();
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

        return Ok(await noticeRepository.GetNotices(game.Id, token));
    }

    /// <summary>
    /// 获取比赛提交
    /// </summary>
    /// <remarks>
    /// 获取比赛提交数据，需要观察者权限
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
    /// 获取比赛实例列表
    /// </summary>
    /// <remarks>
    /// 获取比赛实例数据，需要观察者权限
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
    /// <response code="400">操作无效</response>
    /// <response code="404">比赛未找到</response>
    [RequireUser]
    [HttpGet("{id}/Challenges")]
    [ProducesResponseType(typeof(IDictionary<string, IEnumerable<ChallengeInfo>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Challenges([FromRoute] int id, CancellationToken token)
    {
        var context = await GetContextInfo(id, token);

        if (context.Result is not null)
            return context.Result;

        return Ok((await gameRepository.GetScoreboard(context.Game!, token)).Challenges);
    }

    /// <summary>
    /// 获取全部比赛参与信息
    /// </summary>
    /// <remarks>
    /// 获取比赛的全部题目参与信息，需要Admin权限
    /// </remarks>
    /// <param name="id">比赛id</param>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛参与信息</response>
    /// <response code="400">操作无效</response>
    /// <response code="404">比赛未找到</response>
    [RequireAdmin]
    [HttpGet("{id}/Participations")]
    [ProducesResponseType(typeof(IEnumerable<ParticipationInfoModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Participations([FromRoute] int id, [FromQuery] int count = 20, [FromQuery] int skip = 0, CancellationToken token = default)
    {
        var context = await GetContextInfo(id, token);

        if (context.Result is not null)
            return context.Result;

        return Ok((await participationRepository.GetParticipations(context.Game!, count, skip, token))
                    .Select(p => ParticipationInfoModel.FromParticipation(p)));
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
    /// <response code="400">操作无效</response>
    /// <response code="404">比赛未找到</response>
    [RequireUser]
    [HttpGet("{id}/Challenges/{challengeId}")]
    [ProducesResponseType(typeof(ChallengeDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChallenge([FromRoute] int id, [FromRoute] int challengeId, CancellationToken token)
    {
        var context = await GetContextInfo(id, token);

        if (context.Result is not null)
            return context.Result;

        var instance = await instanceRepository.GetInstance(context.Participation!, challengeId, token);

        if (instance is null)
            return NotFound(new RequestResponse("题目未找到", 404));

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
    /// <response code="400">操作无效</response>
    /// <response code="404">比赛未找到</response>
    [RequireUser]
    [HttpPost("{id}/Challenges/{challengeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submit([FromRoute] int id, [FromRoute] int challengeId, [FromBody] string flag, CancellationToken token)
    {
        var context = await GetContextInfo(id, token);

        if (context.Result is not null)
            return context.Result;

        Submission submission = new()
        {
            ChallengeId = challengeId,
            Answer = flag,
            Game = context.Game!,
            User = context.User,
            Participation = context.Participation!,
            Status = AnswerResult.FlagSubmitted,
            SubmitTimeUTC = DateTimeOffset.UtcNow,
        };

        submission = await submissionRepository.AddSubmission(submission, token);

        // send to flag checker service
        await checkerChannelWriter.WriteAsync(submission, token);

        return Ok(submission.Id);
    }

    /// <summary>
    /// 查询 flag 状态
    /// </summary>
    /// <remarks>
    /// 查询 flag 状态，需要User权限
    /// </remarks>
    /// <param name="id">比赛id</param>
    /// <param name="challengeId">题目id</param>
    /// <param name="submitId">提交id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛提交状态</response>
    /// <response code="404">提交未找到</response>
    [RequireUser]
    [HttpGet("{id}/Status/{submitId}")]
    [ProducesResponseType(typeof(AnswerResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Status([FromRoute] int id, [FromRoute] int challengeId, [FromRoute] int submitId, CancellationToken token)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var submission = await submissionRepository.GetSubmission(id, challengeId, userId, submitId, token);

        if (submission is null)
            return NotFound(new RequestResponse("提交未找到", 404));

        return Ok(submission.Status switch
        {
            AnswerResult.CheatDetected => AnswerResult.WrongAnswer,
            var x => x,
        });
    }

    /// <summary>
    /// 创建容器
    /// </summary>
    /// <remarks>
    /// 创建容器，需要User权限
    /// </remarks>
    /// <param name="id">比赛id</param>
    /// <param name="challengeId">题目id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛题目信息</response>
    /// <response code="404">题目未找到</response>
    /// <response code="400">题目不可创建容器</response>
    [RequireUser]
    [HttpPost("{id}/Container/{challengeId}")]
    [ProducesResponseType(typeof(ContainerInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateContainer([FromRoute] int id, [FromRoute] int challengeId, CancellationToken token)
    {
        var context = await GetContextInfo(id, token);

        if (context.Result is not null)
            return context.Result;

        var instance = await instanceRepository.GetInstance(context.Participation!, challengeId, token);

        if (instance is null)
            return NotFound(new RequestResponse("题目未找到", 404));

        if (!instance.Challenge.Type.IsContainer())
            return BadRequest(new RequestResponse("题目不可创建容器", 400));

        if (instance.Container is not null)
        {
            if (instance.Container.Status == ContainerStatus.Running)
                return BadRequest(new RequestResponse("题目已经创建容器", 400));

            await containerRepository.RemoveContainer(instance.Container, token);
        }

        return await instanceRepository.CreateContainer(instance, token) switch
        {
            null or (TaskStatus.Fail, null) => BadRequest(new RequestResponse("题目创建容器失败", 400)),
            (TaskStatus.Denied, null) => BadRequest(new RequestResponse("队伍容器数目到达上限", 400)),
            (TaskStatus.Success, var x) => Ok(ContainerInfoModel.FromContainer(x!)),
            _ => throw new NotImplementedException(),
        };
    }

    /// <summary>
    /// 获取容器信息
    /// </summary>
    /// <remarks>
    /// 获取容器信息，需要User权限
    /// </remarks>
    /// <param name="id">比赛id</param>
    /// <param name="challengeId">题目id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛题目容器信息</response>
    /// <response code="404">题目未找到</response>
    /// <response code="400">题目不可创建容器</response>
    [RequireUser]
    [HttpGet("{id}/Container/{challengeId}")]
    [ProducesResponseType(typeof(ContainerInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetContainer([FromRoute] int id, [FromRoute] int challengeId, CancellationToken token)
    {
        var context = await GetContextInfo(id, token);

        if (context.Result is not null)
            return context.Result;

        var instance = await instanceRepository.GetInstance(context.Participation!, challengeId, token);

        if (instance is null)
            return NotFound(new RequestResponse("题目未找到", 404));

        if (!instance.Challenge.Type.IsContainer())
            return BadRequest(new RequestResponse("题目不可创建容器", 400));

        if (instance.Container is null)
            return BadRequest(new RequestResponse("题目未创建容器", 400));

        return Ok(ContainerInfoModel.FromContainer(instance.Container));
    }

    /// <summary>
    /// 延长容器时间
    /// </summary>
    /// <remarks>
    /// 延长容器时间，需要User权限，且只能在到期前十分钟延期两小时
    /// </remarks>
    /// <param name="id">比赛id</param>
    /// <param name="challengeId">题目id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛题目容器信息</response>
    /// <response code="404">题目未找到</response>
    /// <response code="400">容器未创建或无法延期</response>
    [RequireUser]
    [HttpPost("{id}/Container/{challengeId}/Prolong")]
    [ProducesResponseType(typeof(ContainerInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProlongContainer([FromRoute] int id, [FromRoute] int challengeId, CancellationToken token)
    {
        var context = await GetContextInfo(id, token);

        if (context.Result is not null)
            return context.Result;

        var instance = await instanceRepository.GetInstance(context.Participation!, challengeId, token);

        if (instance is null)
            return NotFound(new RequestResponse("题目未找到", 404));

        if (!instance.Challenge.Type.IsContainer())
            return BadRequest(new RequestResponse("题目不可创建容器", 400));

        if (instance.Container is null)
            return BadRequest(new RequestResponse("题目未创建容器", 400));

        if (instance.Container.ExpectStopAt <= DateTimeOffset.UtcNow.AddMinutes(-10))
            return BadRequest(new RequestResponse("容器时间尚不可延长", 400));

        await instanceRepository.ProlongContainer(instance.Container, TimeSpan.FromHours(2), token);

        return Ok(ContainerInfoModel.FromContainer(instance.Container));
    }

    /// <summary>
    /// 删除容器
    /// </summary>
    /// <remarks>
    /// 删除，需要User权限
    /// </remarks>
    /// <param name="id">比赛id</param>
    /// <param name="challengeId">题目id</param>
    /// <param name="token"></param>
    /// <response code="200">删除容器成功</response>
    /// <response code="404">题目未找到</response>
    /// <response code="400">题目不可创建容器</response>
    [RequireUser]
    [HttpDelete("{id}/Container/{challengeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteContainer([FromRoute] int id, [FromRoute] int challengeId, CancellationToken token)
    {
        var context = await GetContextInfo(id, token);

        if (context.Result is not null)
            return context.Result;

        var instance = await instanceRepository.GetInstance(context.Participation!, challengeId, token);

        if (instance is null)
            return NotFound(new RequestResponse("题目未找到", 404));

        if (!instance.Challenge.Type.IsContainer())
            return BadRequest(new RequestResponse("题目不可创建容器", 400));

        if (instance.Container is null)
            return BadRequest(new RequestResponse("题目未创建容器", 400));

        return await instanceRepository.DestoryContainer(instance.Container, token) switch
        {
            true => Ok(),
            false => BadRequest(new RequestResponse("题目删除容器失败", 400))
        };
    }

    private class ContextInfo
    {
        public Game? Game = default!;
        public UserInfo? User = default!;
        public Participation? Participation = default!;
        public IActionResult? Result = null;

        public ContextInfo WithResult(IActionResult res)
        {
            Result = res;
            return this;
        }
    };

    private async Task<ContextInfo> GetContextInfo(int id, CancellationToken token = default)
    {
        ContextInfo res = new()
        {
            User = await userManager.GetUserAsync(User),
            Game = await gameRepository.GetGameById(id, token)
        };

        if (res.Game is null)
            return res.WithResult(NotFound(new RequestResponse("比赛未找到")));

        if (res.User!.ActiveTeam is null)
            return res.WithResult(BadRequest(new RequestResponse("请激活一个队伍以参赛")));

        var part = await participationRepository.GetParticipation(res.User.ActiveTeam, res.Game, token);

        if (part is null)
            return res.WithResult(BadRequest(new RequestResponse("您尚未参赛")));

        if (part.Status != ParticipationStatus.Accepted)
            return res.WithResult(BadRequest(new RequestResponse("您的参赛申请尚未通过或被禁赛")));

        res.Participation = part;

        if (DateTimeOffset.UtcNow < res.Game.StartTimeUTC)
            return res.WithResult(BadRequest(new RequestResponse("比赛还未开始")));

        return res;
    }
}