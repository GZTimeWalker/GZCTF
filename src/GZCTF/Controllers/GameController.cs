using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Channels;
using GZCTF.Middlewares;
using GZCTF.Models.Request.Admin;
using GZCTF.Models.Request.Edit;
using GZCTF.Models.Request.Game;
using GZCTF.Repositories;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GZCTF.Controllers;

/// <summary>
/// 比赛数据交互接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class GameController : ControllerBase
{
    private readonly ILogger<GameController> logger;
    private readonly UserManager<UserInfo> userManager;
    private readonly ChannelWriter<Submission> checkerChannelWriter;
    private readonly IFileRepository fileService;
    private readonly IGameRepository gameRepository;
    private readonly ITeamRepository teamRepository;
    private readonly IGameEventRepository eventRepository;
    private readonly IInstanceRepository instanceRepository;
    private readonly IGameNoticeRepository noticeRepository;
    private readonly IGameEventRepository gameEventRepository;
    private readonly IContainerRepository containerRepository;
    private readonly ICheatInfoRepository cheatInfoRepository;
    private readonly IChallengeRepository challengeRepository;
    private readonly ISubmissionRepository submissionRepository;
    private readonly IParticipationRepository participationRepository;

    public GameController(
        ILogger<GameController> _logger,
        UserManager<UserInfo> _userManager,
        ChannelWriter<Submission> _channelWriter,
        IFileRepository _fileService,
        IGameRepository _gameRepository,
        ITeamRepository _teamRepository,
        IGameEventRepository _eventRepository,
        IGameNoticeRepository _noticeRepository,
        IInstanceRepository _instanceRepository,
        ICheatInfoRepository _cheatInfoRepository,
        IChallengeRepository _challengeRepository,
        IContainerRepository _containerRepository,
        IGameEventRepository _gameEventRepository,
        ISubmissionRepository _submissionRepository,
        IParticipationRepository _participationRepository)
    {
        logger = _logger;
        userManager = _userManager;
        checkerChannelWriter = _channelWriter;
        fileService = _fileService;
        gameRepository = _gameRepository;
        teamRepository = _teamRepository;
        eventRepository = _eventRepository;
        noticeRepository = _noticeRepository;
        instanceRepository = _instanceRepository;
        challengeRepository = _challengeRepository;
        containerRepository = _containerRepository;
        gameEventRepository = _gameEventRepository;
        cheatInfoRepository = _cheatInfoRepository;
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
    /// <param name="id">比赛Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛信息</response>
    /// <response code="404">比赛未找到</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DetailedGameInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Games(int id, CancellationToken token)
    {
        var context = await GetContextInfo(id, token: token);

        if (context.Game is null)
            return NotFound(new RequestResponse("比赛未找到"));

        var count = await participationRepository.GetParticipationCount(context.Game, token);

        return Ok(DetailedGameInfoModel.FromGame(context.Game, count)
                      .WithParticipation(context.Participation));
    }

    /// <summary>
    /// 加入一个比赛
    /// </summary>
    /// <remarks>
    /// 加入一场比赛，需要User权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">成功加入比赛</response>
    /// <response code="403">无权操作或操作无效</response>
    /// <response code="404">比赛未找到</response>
    [RequireUser]
    [HttpPost("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> JoinGame(int id, [FromBody] GameJoinModel model, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        if (!game.PracticeMode && game.EndTimeUTC < DateTimeOffset.UtcNow)
            return BadRequest(new RequestResponse("比赛已结束"));

        if (!string.IsNullOrEmpty(game.InviteCode) && game.InviteCode != model.InviteCode)
            return BadRequest(new RequestResponse("比赛邀请码错误"));

        if (game.Organizations is { Count: > 0 } && game.Organizations.All(o => o != model.Organization))
            return BadRequest(new RequestResponse("无效的参赛单位"));

        var user = await userManager.GetUserAsync(User);
        var team = await teamRepository.GetTeamById(model.TeamId, token);

        if (team is null)
            return NotFound(new RequestResponse("队伍未找到", 404));

        if (team.Members.All(u => u.Id != user!.Id))
            return BadRequest(new RequestResponse("您不是此队伍的队员"));

        // 如果已经报名（非拒绝状态）
        if (await participationRepository.CheckRepeatParticipation(user!, game, token))
            return BadRequest(new RequestResponse("您已经在其他队伍报名参赛"));

        // 移除所有的已经存在的报名
        await participationRepository.RemoveUserParticipations(user!, game, token);

        // 根据队伍获取报名信息
        var part = await participationRepository.GetParticipation(team, game, token);

        // 如果队伍未报名
        if (part is null)
        {
            // 创建新的队伍参与对象，不添加三元组
            part = new()
            {
                Game = game,
                Team = team,
                Organization = model.Organization,
                Token = gameRepository.GetToken(game, team)
            };

            participationRepository.Add(part);
        }

        if (game.TeamMemberCountLimit > 0 && part.Members.Count >= game.TeamMemberCountLimit)
            return BadRequest(new RequestResponse("队伍参与人数超过比赛限制"));

        // 报名当前成员
        part.Members.Add(new(user!, game, team));

        part.Organization = model.Organization;

        if (part.Status == ParticipationStatus.Rejected)
            part.Status = ParticipationStatus.Pending;

        await participationRepository.SaveAsync(token);

        if (game.AcceptWithoutReview)
            await participationRepository.UpdateParticipationStatus(part, ParticipationStatus.Accepted, token);

        logger.Log($"[{team!.Name}] 成功报名了比赛 [{game.Title}]", user, TaskStatus.Success);

        return Ok();
    }

    /// <summary>
    /// 退出一个比赛
    /// </summary>
    /// <remarks>
    /// 退出一场比赛，需要User权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功退出比赛</response>
    /// <response code="403">无权操作或操作无效</response>
    /// <response code="404">比赛未找到</response>
    [RequireUser]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LeaveGame(int id, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        var user = await userManager.GetUserAsync(User);

        var part = await participationRepository.GetParticipation(user!, game, token);

        if (part is null || part.Members.All(u => u.UserId != user!.Id))
            return BadRequest(new RequestResponse("无法退出未报名的比赛"));

        if (part.Status != ParticipationStatus.Pending && part.Status != ParticipationStatus.Rejected)
            return BadRequest(new RequestResponse("审核通过后无法退出比赛"));

        // FIXME: 审核通过后可以添加新用户、但不能退出？

        part.Members.RemoveWhere(u => u.UserId == user!.Id);

        if (part.Members.Count == 0)
            await participationRepository.RemoveParticipation(part, token);
        else
            await participationRepository.SaveAsync(token);

        return Ok();
    }

    /// <summary>
    /// 获取积分榜
    /// </summary>
    /// <remarks>
    /// 获取积分榜数据
    /// </remarks>
    /// <param name="id">比赛Id</param>
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
            return BadRequest(new RequestResponse("比赛未开始"));

        return Ok(await gameRepository.GetScoreboard(game, token));
    }

    /// <summary>
    /// 获取比赛通知
    /// </summary>
    /// <remarks>
    /// 获取比赛通知数据
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛通知</response>
    /// <response code="400">比赛未找到</response>
    [HttpGet("{id}/Notices")]
    [ProducesResponseType(typeof(GameNotice[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Notices([FromRoute] int id, [FromQuery] int count = 100, [FromQuery] int skip = 0, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到"));

        if (DateTimeOffset.UtcNow < game.StartTimeUTC)
            return BadRequest(new RequestResponse("比赛未开始"));

        return Ok(await noticeRepository.GetNotices(game.Id, count, skip, token));
    }

    /// <summary>
    /// 获取比赛事件
    /// </summary>
    /// <remarks>
    /// 获取比赛事件数据，需要Monitor权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="count"></param>
    /// <param name="hideContainer">隐藏容器</param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛事件</response>
    /// <response code="400">比赛未找到</response>
    [RequireMonitor]
    [HttpGet("{id}/Events")]
    [ProducesResponseType(typeof(GameEvent[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Events([FromRoute] int id, [FromQuery] bool hideContainer = false, [FromQuery] int count = 100, [FromQuery] int skip = 0, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到"));

        if (DateTimeOffset.UtcNow < game.StartTimeUTC)
            return BadRequest(new RequestResponse("比赛未开始"));

        return Ok(await eventRepository.GetEvents(game.Id, hideContainer, count, skip, token));
    }

    /// <summary>
    /// 获取比赛提交
    /// </summary>
    /// <remarks>
    /// 获取比赛提交数据，需要Monitor权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="type">提交类型</param>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛提交</response>
    /// <response code="400">比赛未找到</response>
    [RequireMonitor]
    [HttpGet("{id}/Submissions")]
    [ProducesResponseType(typeof(Submission[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submissions([FromRoute] int id, [FromQuery] AnswerResult? type = null, [FromQuery] int count = 100, [FromQuery] int skip = 0, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到"));

        if (DateTimeOffset.UtcNow < game.StartTimeUTC)
            return BadRequest(new RequestResponse("比赛未开始"));

        return Ok(await submissionRepository.GetSubmissions(game, type, count, skip, token));
    }

    /// <summary>
    /// 获取比赛作弊信息
    /// </summary>
    /// <remarks>
    /// 获取比赛作弊数据，需要Monitor权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛作弊数据</response>
    /// <response code="400">比赛未找到</response>
    [RequireMonitor]
    [HttpGet("{id}/CheatInfo")]
    [ProducesResponseType(typeof(CheatInfoModel[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheatInfo([FromRoute] int id, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到"));

        if (DateTimeOffset.UtcNow < game.StartTimeUTC)
            return BadRequest(new RequestResponse("比赛未开始"));

        return Ok((await cheatInfoRepository.GetCheatInfoByGameId(game.Id, token))
            .Select(CheatInfoModel.FromCheatInfo));
    }

    /// <summary>
    /// 获取全部比赛题目信息及当前队伍信息
    /// </summary>
    /// <remarks>
    /// 获取比赛的全部题目，需要User权限，需要当前激活队伍已经报名
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛题目信息</response>
    /// <response code="400">操作无效</response>
    /// <response code="404">比赛未找到</response>
    [RequireUser]
    [HttpGet("{id}/Details")]
    [ProducesResponseType(typeof(GameDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChallengesWithTeamInfo([FromRoute] int id, CancellationToken token)
    {
        var context = await GetContextInfo(id, token: token);

        if (context.Result is not null)
            return context.Result;

        var scoreboard = await gameRepository.GetScoreboard(context.Game!, token);

        var boarditem = scoreboard.Items.FirstOrDefault(i => i.Id == context.Participation!.TeamId);

        // make sure team info is not null
        boarditem ??= new ScoreboardItem()
        {
            Avatar = context.Participation!.Team.AvatarUrl,
            SolvedCount = 0,
            Rank = 0,
            Name = context.Participation!.Team.Name,
            Id = context.Participation!.TeamId
        };

        return Ok(new GameDetailModel()
        {
            ScoreboardItem = boarditem,
            TeamToken = context.Participation!.Token,
            Challenges = scoreboard.Challenges,
            WriteupDeadline = context.Game!.WriteupDeadline
        });
    }

    /// <summary>
    /// 获取全部比赛参与信息
    /// </summary>
    /// <remarks>
    /// 获取比赛的全部题目参与信息，需要Admin权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛参与信息</response>
    /// <response code="400">操作无效</response>
    /// <response code="404">比赛未找到</response>
    [RequireAdmin]
    [HttpGet("{id}/Participations")]
    [ProducesResponseType(typeof(ParticipationInfoModel[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Participations([FromRoute] int id, CancellationToken token = default)
    {
        var context = await GetContextInfo(id, token: token);

        if (context.Game is null)
            return NotFound(new RequestResponse("比赛未找到"));

        return Ok((await participationRepository.GetParticipations(context.Game!, token))
                    .Select(ParticipationInfoModel.FromParticipation));
    }

    /// <summary>
    /// 下载比赛积分榜
    /// </summary>
    /// <remarks>
    /// 下载比赛积分榜，需要Monitor权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功下载比赛积分榜</response>
    /// <response code="400">操作无效</response>
    /// <response code="404">比赛未找到</response>
    [RequireMonitor]
    [HttpGet("{id}/ScoreboardSheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ScoreboardSheet([FromRoute] int id, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到"));

        if (DateTimeOffset.UtcNow < game.StartTimeUTC)
            return BadRequest(new RequestResponse("比赛未开始"));

        try
        {
            var scoreboard = await gameRepository.GetScoreboardWithMembers(game, token);
            var stream = ExcelHelper.GetScoreboardExcel(scoreboard, game);
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{game.Title}_Scoreboard_{DateTimeOffset.Now:yyyyMMddHHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            logger.SystemLog("下载积分榜遇到错误", TaskStatus.Failed, LogLevel.Error);
            logger.LogError(ex, ex.Message);
            return BadRequest(new RequestResponse("遇到问题，请重试"));
        }
    }

    /// <summary>
    /// 下载比赛全部提交
    /// </summary>
    /// <remarks>
    /// 下载比赛全部提交，需要Monitor权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功下载比赛全部提交</response>
    /// <response code="400">操作无效</response>
    /// <response code="404">比赛未找到</response>
    [RequireMonitor]
    [HttpGet("{id}/SubmissionSheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmissionSheet([FromRoute] int id, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到"));

        if (DateTimeOffset.UtcNow < game.StartTimeUTC)
            return BadRequest(new RequestResponse("比赛未开始"));

        var submissions = await submissionRepository.GetSubmissions(game, count: 0, token: token);

        var stream = ExcelHelper.GetSubmissionExcel(submissions);
        stream.Seek(0, SeekOrigin.Begin);

        return File(stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{game.Title}_Submissions_{DateTimeOffset.Now:yyyyMMddHHmmss}.xlsx");
    }

    /// <summary>
    /// 获取比赛题目信息
    /// </summary>
    /// <remarks>
    /// 获取比赛题目信息，需要User权限，需要当前激活队伍已经报名
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="challengeId">题目Id</param>
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
        if (id <= 0 || challengeId <= 0)
            return NotFound(new RequestResponse("题目未找到", 404));

        var context = await GetContextInfo(id, token: token);

        if (context.Result is not null)
            return context.Result;

        var instance = await instanceRepository.GetInstance(context.Participation!, challengeId, token);

        if (instance is null)
            return NotFound(new RequestResponse("题目未找到或动态附件分配失败", 404));

        return Ok(ChallengeDetailModel.FromInstance(instance));
    }

    /// <summary>
    /// 提交 flag
    /// </summary>
    /// <remarks>
    /// 提交 flag，需要User权限，需要当前激活队伍已经报名
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="challengeId">题目Id</param>
    /// <param name="model">提交Flag</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛题目信息</response>
    /// <response code="400">操作无效</response>
    /// <response code="404">比赛未找到</response>
    [RequireUser]
    [HttpPost("{id}/Challenges/{challengeId}")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Submit))]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submit([FromRoute] int id, [FromRoute] int challengeId, [FromBody] FlagSubmitModel model, CancellationToken token)
    {
        var context = await GetContextInfo(id, challengeId, false, token: token);

        if (context.Result is not null)
            return context.Result;

        Submission submission = new()
        {
            Answer = model.Flag.Trim(),
            Game = context.Game!,
            User = context.User!,
            Challenge = context.Challenge!,
            Team = context.Participation!.Team,
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
    /// <param name="id">比赛Id</param>
    /// <param name="challengeId">题目Id</param>
    /// <param name="submitId">提交id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛提交状态</response>
    /// <response code="404">提交未找到</response>
    [RequireUser]
    [HttpGet("{id}/Challenges/{challengeId}/Status/{submitId}")]
    [ProducesResponseType(typeof(AnswerResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Status([FromRoute] int id, [FromRoute] int challengeId, [FromRoute] int submitId, CancellationToken token)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var submission = await submissionRepository.GetSubmission(id, challengeId, userId!, submitId, token);

        if (submission is null)
            return NotFound(new RequestResponse("提交未找到", 404));

        return Ok(submission.Status switch
        {
            AnswerResult.CheatDetected => AnswerResult.WrongAnswer,
            var x => x,
        });
    }

    /// <summary>
    /// 获取 Writeup 信息
    /// </summary>
    /// <remarks>
    /// 获取赛后题解提交情况，需要User权限
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <response code="200">成功提交 Writeup </response>
    /// <response code="400">提交不符合要求</response>
    /// <response code="404">比赛未找到</response>
    [RequireUser]
    [HttpGet("{id}/Writeup")]
    [ProducesResponseType(typeof(BasicWriteupInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetWriteup([FromRoute] int id, CancellationToken token)
    {
        var context = await GetContextInfo(id, denyAfterEnded: false, token: token);

        if (context.Result is not null)
            return context.Result;

        return Ok(BasicWriteupInfoModel.FromParticipation(context.Participation!));
    }

    /// <summary>
    /// 提交 Writeup
    /// </summary>
    /// <remarks>
    /// 提交赛后题解，需要User权限
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="file">文件</param>
    /// <param name="token"></param>
    /// <response code="200">成功提交 Writeup </response>
    /// <response code="400">提交不符合要求</response>
    /// <response code="404">比赛未找到</response>
    [RequireUser]
    [HttpPost("{id}/Writeup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitWriteup([FromRoute] int id, IFormFile file, CancellationToken token)
    {
        if (file.Length == 0)
            return BadRequest(new RequestResponse("文件非法"));

        if (file.Length > 20 * 1024 * 1024)
            return BadRequest(new RequestResponse("文件过大"));

        if (file.ContentType != "application/pdf" || Path.GetExtension(file.FileName) != ".pdf")
            return BadRequest(new RequestResponse("请上传 pdf 文件"));

        var context = await GetContextInfo(id, denyAfterEnded: false, token: token);

        if (context.Result is not null)
            return context.Result;

        var game = context.Game!;
        var part = context.Participation!;
        var team = part.Team;

        if (DateTimeOffset.UtcNow > game.WriteupDeadline)
            return BadRequest(new RequestResponse("提交截止时间已过"));

        var wp = context.Participation!.Writeup;

        if (wp is not null)
            await fileService.DeleteFile(wp, token);

        wp = await fileService.CreateOrUpdateFile(file, $"Writeup-{game.Id}-{team.Id}-{DateTimeOffset.Now:yyyyMMdd-HH.mm.ssZ}.pdf", token);

        if (wp is null)
            return BadRequest(new RequestResponse("保存文件失败"));

        part.Writeup = wp;

        await participationRepository.SaveAsync(token);

        logger.Log($"{team.Name} 成功提交 {game.Title} 的 Writeup", context.User!, TaskStatus.Success);

        return Ok();
    }

    /// <summary>
    /// 创建容器
    /// </summary>
    /// <remarks>
    /// 创建容器，需要User权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="challengeId">题目Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛题目信息</response>
    /// <response code="404">题目未找到</response>
    /// <response code="400">题目不可创建容器</response>
    [RequireUser]
    [HttpPost("{id}/Container/{challengeId}")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Container))]
    [ProducesResponseType(typeof(ContainerInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreateContainer([FromRoute] int id, [FromRoute] int challengeId, CancellationToken token)
    {
        var context = await GetContextInfo(id, token: token);

        if (context.Result is not null)
            return context.Result;

        var instance = await instanceRepository.GetInstance(context.Participation!, challengeId, token);

        if (instance is null)
            return NotFound(new RequestResponse("题目未找到", 404));

        if (!instance.Challenge.Type.IsContainer())
            return BadRequest(new RequestResponse("题目不可创建容器"));

        if (DateTimeOffset.UtcNow - instance.LastContainerOperation < TimeSpan.FromSeconds(10))
            return new JsonResult(new RequestResponse("容器操作过于频繁", 429))
            {
                StatusCode = 429
            };

        if (instance.Container is not null)
        {
            if (instance.Container.Status == ContainerStatus.Running)
                return BadRequest(new RequestResponse("题目已经创建容器"));

            await containerRepository.RemoveContainer(instance.Container, token);
        }

        return await instanceRepository.CreateContainer(instance, context.Participation!.Team, context.User!, context.Game!.ContainerCountLimit, token) switch
        {
            null or (TaskStatus.Failed, null) => BadRequest(new RequestResponse("题目创建容器失败")),
            (TaskStatus.Denied, null) => BadRequest(new RequestResponse($"队伍容器数目不能超过 {context.Game.ContainerCountLimit}")),
            (TaskStatus.Success, var x) => Ok(ContainerInfoModel.FromContainer(x!)),
            _ => throw new NotImplementedException(),
        };
    }

    /// <summary>
    /// 延长容器时间
    /// </summary>
    /// <remarks>
    /// 延长容器时间，需要User权限，且只能在到期前十分钟延期两小时
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="challengeId">题目Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取比赛题目容器信息</response>
    /// <response code="404">题目未找到</response>
    /// <response code="400">容器未创建或无法延期</response>
    [RequireUser]
    [HttpPost("{id}/Container/{challengeId}/Prolong")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Container))]
    [ProducesResponseType(typeof(ContainerInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProlongContainer([FromRoute] int id, [FromRoute] int challengeId, CancellationToken token)
    {
        var context = await GetContextInfo(id, token: token);

        if (context.Result is not null)
            return context.Result;

        var instance = await instanceRepository.GetInstance(context.Participation!, challengeId, token);

        if (instance is null)
            return NotFound(new RequestResponse("题目未找到", 404));

        if (!instance.Challenge.Type.IsContainer())
            return BadRequest(new RequestResponse("题目不可创建容器"));

        if (instance.Container is null)
            return BadRequest(new RequestResponse("题目未创建容器"));

        if (instance.Container.ExpectStopAt - DateTimeOffset.UtcNow > TimeSpan.FromMinutes(10))
            return BadRequest(new RequestResponse("容器时间尚不可延长"));

        await instanceRepository.ProlongContainer(instance.Container, TimeSpan.FromHours(2), token);

        return Ok(ContainerInfoModel.FromContainer(instance.Container));
    }

    /// <summary>
    /// 删除容器
    /// </summary>
    /// <remarks>
    /// 删除，需要User权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="challengeId">题目Id</param>
    /// <param name="token"></param>
    /// <response code="200">删除容器成功</response>
    /// <response code="404">题目未找到</response>
    /// <response code="400">题目不可创建容器</response>
    [RequireUser]
    [HttpDelete("{id}/Container/{challengeId}")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Container))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DeleteContainer([FromRoute] int id, [FromRoute] int challengeId, CancellationToken token)
    {
        var context = await GetContextInfo(id, token: token);

        if (context.Result is not null)
            return context.Result;

        var instance = await instanceRepository.GetInstance(context.Participation!, challengeId, token);

        if (instance is null)
            return NotFound(new RequestResponse("题目未找到", 404));

        if (!instance.Challenge.Type.IsContainer())
            return BadRequest(new RequestResponse("题目不可创建容器"));

        if (instance.Container is null)
            return BadRequest(new RequestResponse("题目未创建容器"));

        if (DateTimeOffset.UtcNow - instance.LastContainerOperation < TimeSpan.FromSeconds(10))
            return new JsonResult(new RequestResponse("容器操作过于频繁", 429))
            {
                StatusCode = 429
            };

        var destroyId = instance.Container.ContainerId;

        if (!await instanceRepository.DestroyContainer(instance.Container, token))
            return BadRequest(new RequestResponse("题目删除容器失败"));

        instance.LastContainerOperation = DateTimeOffset.UtcNow;

        await gameEventRepository.AddEvent(new()
        {
            Type = EventType.ContainerDestroy,
            GameId = context.Game!.Id,
            TeamId = context.Participation!.TeamId,
            UserId = context.User!.Id,
            Content = $"{instance.Challenge.Title}#{instance.Challenge.Id} 销毁容器实例"
        }, token);

        logger.Log($"{context.Participation!.Team.Name} 销毁题目 {instance.Challenge.Title} 的容器实例 [{destroyId}]", context.User, TaskStatus.Success);

        return Ok();
    }

    private class ContextInfo
    {
        public Game? Game = default!;
        public UserInfo? User = default!;
        public Challenge? Challenge = default!;
        public Participation? Participation = default!;
        public IActionResult? Result = null;

        public ContextInfo WithResult(IActionResult res)
        {
            Result = res;
            return this;
        }
    };

    private async Task<ContextInfo> GetContextInfo(int id, int challengeId = 0, bool withFlag = false, bool denyAfterEnded = true, CancellationToken token = default)
    {
        ContextInfo res = new()
        {
            User = await userManager.GetUserAsync(User),
            Game = await gameRepository.GetGameById(id, token)
        };

        if (res.Game is null)
            return res.WithResult(NotFound(new RequestResponse("比赛未找到", 404)));

        var part = await participationRepository.GetParticipation(res.User!, res.Game, token);

        if (part is null)
            return res.WithResult(BadRequest(new RequestResponse("您尚未参赛")));

        res.Participation = part;

        if (part.Status != ParticipationStatus.Accepted)
            return res.WithResult(BadRequest(new RequestResponse("您的参赛申请尚未通过或被禁赛")));

        if (DateTimeOffset.UtcNow < res.Game.StartTimeUTC)
            return res.WithResult(BadRequest(new RequestResponse("比赛未开始")));

        if (denyAfterEnded && !res.Game.PracticeMode && res.Game.EndTimeUTC < DateTimeOffset.UtcNow)
            return res.WithResult(BadRequest(new RequestResponse("比赛已结束")));

        if (challengeId > 0)
        {
            var challenge = await challengeRepository.GetChallenge(id, challengeId, withFlag, token);

            if (challenge is null)
                return res.WithResult(NotFound(new RequestResponse("题目未找到", 404)));

            res.Challenge = challenge;
        }

        return res;
    }
}
