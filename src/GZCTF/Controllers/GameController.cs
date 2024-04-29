using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Channels;
using GZCTF.Middlewares;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Admin;
using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

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
public class GameController(
    ILogger<GameController> logger,
    UserManager<UserInfo> userManager,
    ChannelWriter<Submission> channelWriter,
    IFileRepository fileService,
    IGameRepository gameRepository,
    ITeamRepository teamRepository,
    IGameEventRepository eventRepository,
    IGameNoticeRepository noticeRepository,
    ICheatInfoRepository cheatInfoRepository,
    IContainerRepository containerRepository,
    IGameEventRepository gameEventRepository,
    ISubmissionRepository submissionRepository,
    IGameChallengeRepository challengeRepository,
    IGameInstanceRepository gameInstanceRepository,
    IParticipationRepository participationRepository,
    IOptionsSnapshot<ContainerPolicy> containerPolicy,
    IStringLocalizer<Program> localizer) : ControllerBase
{
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
    public async Task<IActionResult> Games(CancellationToken token) =>
        Ok(await gameRepository.GetBasicGameInfo(10, 0, token));

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
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DetailedGameInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Games(int id, CancellationToken token)
    {
        ContextInfo context = await GetContextInfo(id, token: token);

        if (context.Game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

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
    [HttpPost("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> JoinGame(int id, [FromBody] GameJoinModel model, CancellationToken token)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        if (!game.PracticeMode && game.EndTimeUtc < DateTimeOffset.UtcNow)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_Ended)], ErrorCodes.GameEnded));

        if (!string.IsNullOrEmpty(game.InviteCode) && game.InviteCode != model.InviteCode)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_InvalidInvitationCode)]));

        if (game.Organizations is { Count: > 0 } && game.Organizations.All(o => o != model.Organization))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_InvalidOrganization)]));

        UserInfo? user = await userManager.GetUserAsync(User);
        Team? team = await teamRepository.GetTeamById(model.TeamId, token);

        if (team is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Team_NotFound)],
                StatusCodes.Status404NotFound));

        if (team.Members.All(u => u.Id != user!.Id))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotMemberOfTeam)]));

        // 如果已经报名（非拒绝状态）
        if (await participationRepository.CheckRepeatParticipation(user!, game, token))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_InOtherTeam)]));

        // 移除所有的已经存在的报名
        await participationRepository.RemoveUserParticipations(user!, game, token);

        // 根据队伍获取报名信息
        Participation? part = await participationRepository.GetParticipation(team, game, token);

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
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_TeamMemberLimitExceeded)]));

        // 报名当前成员
        part.Members.Add(new(user!, game, team));

        part.Organization = model.Organization;

        if (part.Status == ParticipationStatus.Rejected)
            part.Status = ParticipationStatus.Pending;

        await participationRepository.SaveAsync(token);

        if (game.AcceptWithoutReview)
            await participationRepository.UpdateParticipationStatus(part, ParticipationStatus.Accepted, token);

        logger.Log(Program.StaticLocalizer[nameof(Resources.Program.Game_JoinSucceeded), team.Name, game.Title], user,
            TaskStatus.Success);

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
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LeaveGame(int id, CancellationToken token)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        UserInfo? user = await userManager.GetUserAsync(User);

        Participation? part = await participationRepository.GetParticipation(user!, game, token);

        if (part is null || part.Members.All(u => u.UserId != user!.Id))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_CannotLeaveWithoutJoin)]));

        if (part.Status != ParticipationStatus.Pending && part.Status != ParticipationStatus.Rejected)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_CannotLeaveAfterApproval)]));

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
    [HttpGet("{id:int}/Scoreboard")]
    [ProducesResponseType(typeof(ScoreboardModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Scoreboard([FromRoute] int id, CancellationToken token)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        if (DateTimeOffset.UtcNow < game.StartTimeUtc)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted)]));

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
    [HttpGet("{id:int}/Notices")]
    [ProducesResponseType(typeof(GameNotice[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Notices([FromRoute] int id, [FromQuery] int count = 100, [FromQuery] int skip = 0,
        CancellationToken token = default)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        if (DateTimeOffset.UtcNow < game.StartTimeUtc)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted)]));

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
    [HttpGet("{id:int}/Events")]
    [ProducesResponseType(typeof(GameEvent[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Events([FromRoute] int id, [FromQuery] bool hideContainer = false,
        [FromQuery] int count = 100, [FromQuery] int skip = 0, CancellationToken token = default)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        if (DateTimeOffset.UtcNow < game.StartTimeUtc)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted)]));

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
    [HttpGet("{id:int}/Submissions")]
    [ProducesResponseType(typeof(Submission[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submissions([FromRoute] int id, [FromQuery] AnswerResult? type = null,
        [FromQuery] int count = 100, [FromQuery] int skip = 0, CancellationToken token = default)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        if (DateTimeOffset.UtcNow < game.StartTimeUtc)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted)]));

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
    [HttpGet("{id:int}/CheatInfo")]
    [ProducesResponseType(typeof(CheatInfoModel[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheatInfo([FromRoute] int id, CancellationToken token = default)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        if (DateTimeOffset.UtcNow < game.StartTimeUtc)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted)]));

        return Ok((await cheatInfoRepository.GetCheatInfoByGameId(game.Id, token))
            .Select(CheatInfoModel.FromCheatInfo));
    }

    /// <summary>
    /// 获取开启了流量捕获的比赛题目
    /// </summary>
    /// <remarks>
    /// 获取开启了流量捕获的比赛题目，需要Monitor权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取题目列表</response>
    /// <response code="404">未找到相关捕获信息</response>
    [RequireMonitor]
    [HttpGet("Games/{id:int}/Captures")]
    [ProducesResponseType(typeof(ChallengeTrafficModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChallengesWithTrafficCapturing([FromRoute] int id, CancellationToken token) =>
        Ok((await challengeRepository.GetChallengesWithTrafficCapturing(id, token))
            .Select(ChallengeTrafficModel.FromChallenge));

    /// <summary>
    /// 获取比赛题目中捕获到到队伍信息
    /// </summary>
    /// <remarks>
    /// 获取比赛题目中捕获到到队伍信息，需要Monitor权限
    /// </remarks>
    /// <param name="challengeId">题目 Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取文件列表</response>
    /// <response code="404">未找到相关捕获信息</response>
    [RequireMonitor]
    [HttpGet("Captures/{challengeId:int}")]
    [ProducesResponseType(typeof(TeamTrafficModel[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChallengeTraffic([FromRoute] int challengeId, CancellationToken token)
    {
        var filePath = Path.Combine(FilePath.Capture, $"{challengeId}");

        if (!Path.Exists(filePath))
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_CaptureNotFound)],
                StatusCodes.Status404NotFound));

        List<int> participationIds = await GetDirNamesAsInt(filePath);

        if (participationIds.Count == 0)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_CaptureNotFound)],
                StatusCodes.Status404NotFound));

        Participation[] participation = await participationRepository.GetParticipationsByIds(participationIds, token);

        return Ok(participation.Select(p => TeamTrafficModel.FromParticipation(p, challengeId)));
    }

    /// <summary>
    /// 获取比赛题目中捕获到到队伍的流量包列表
    /// </summary>
    /// <remarks>
    /// 获取比赛题目中捕获到到队伍的流量包列表，需要Monitor权限
    /// </remarks>
    /// <param name="challengeId">题目 Id</param>
    /// <param name="partId">队伍参与 Id</param>
    /// <response code="200">成功获取文件列表</response>
    /// <response code="404">未找到相关捕获信息</response>
    [RequireMonitor]
    [HttpGet("Captures/{challengeId:int}/{partId:int}")]
    [ProducesResponseType(typeof(FileRecord[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetTeamTraffic([FromRoute] int challengeId, [FromRoute] int partId)
    {
        var filePath = Path.Combine(FilePath.Capture, $"{challengeId}", $"{partId}");

        if (!Path.Exists(filePath))
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_CaptureNotFound)],
                StatusCodes.Status404NotFound));

        return Ok(FilePath.GetFileRecords(filePath, out _));
    }

    /// <summary>
    /// 获取流量包文件压缩包
    /// </summary>
    /// <remarks>
    /// 获取流量包文件，需要Monitor权限
    /// </remarks>
    /// <param name="challengeId">题目 Id</param>
    /// <param name="partId">队伍参与 Id</param>
    /// <param name="token">token</param>
    /// <response code="200">成功获取文件</response>
    /// <response code="404">未找到相关捕获信息</response>
    [RequireMonitor]
    [HttpGet("Captures/{challengeId:int}/{partId:int}/All")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAllTeamTraffic([FromRoute] int challengeId, [FromRoute] int partId,
        CancellationToken token)
    {
        var filePath = Path.Combine(FilePath.Capture, $"{challengeId}", $"{partId}");

        if (!Path.Exists(filePath))
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_CaptureNotFound)],
                StatusCodes.Status404NotFound));

        var filename = $"Capture-{challengeId}-{partId}-{DateTimeOffset.UtcNow:yyyyMMdd-HH.mm.ssZ}";
        Stream stream = await Codec.ZipFilesAsync(filePath, filename, token);
        stream.Seek(0, SeekOrigin.Begin);

        return File(stream, "application/zip", $"{filename}.zip");
    }

    /// <summary>
    /// 删除某队伍的全部流量包文件
    /// </summary>
    /// <remarks>
    /// 删除某队伍的流量包文件，需要Monitor权限
    /// </remarks>
    /// <param name="challengeId">题目 Id</param>
    /// <param name="partId">队伍参与 Id</param>
    /// <response code="200">成功获取文件</response>
    /// <response code="404">未找到相关捕获信息</response>
    [RequireMonitor]
    [HttpDelete("Captures/{challengeId:int}/{partId:int}/All")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public IActionResult DeleteAllTeamTraffic([FromRoute] int challengeId, [FromRoute] int partId)
    {
        try
        {
            var filePath = Path.Combine(FilePath.Capture, $"{challengeId}", $"{partId}");

            if (!Path.Exists(filePath))
                return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_CaptureNotFound)],
                    StatusCodes.Status404NotFound));

            Directory.Delete(filePath, true);

            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(new RequestResponse(e.Message));
        }
    }

    /// <summary>
    /// 获取流量包文件
    /// </summary>
    /// <remarks>
    /// 获取流量包文件，需要Monitor权限
    /// </remarks>
    /// <param name="challengeId">题目 Id</param>
    /// <param name="partId">队伍参与 Id</param>
    /// <param name="filename">流量包文件名</param>
    /// <response code="200">成功获取文件</response>
    /// <response code="404">未找到相关捕获信息</response>
    [RequireMonitor]
    [HttpGet("Captures/{challengeId:int}/{partId:int}/{filename}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetTeamTraffic([FromRoute] int challengeId, [FromRoute] int partId,
        [FromRoute] string filename)
    {
        try
        {
            var file = Path.GetFileName(filename);
            var path = Path.GetFullPath(Path.Combine(FilePath.Capture, $"{challengeId}", $"{partId}", file));

            if (Path.GetExtension(file) != ".pcap" || !Path.Exists(path))
                return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_CaptureNotFound)]));

            return new PhysicalFileResult(path, MediaTypeNames.Application.Octet) { FileDownloadName = file };
        }
        catch
        {
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_CaptureNotFound)]));
        }
    }

    /// <summary>
    /// 删除流量包文件
    /// </summary>
    /// <remarks>
    /// 删除流量包文件，需要Monitor权限
    /// </remarks>
    /// <param name="challengeId">题目 Id</param>
    /// <param name="partId">队伍参与 Id</param>
    /// <param name="filename">流量包文件名</param>
    /// <response code="200">成功获取文件</response>
    /// <response code="404">未找到相关捕获信息</response>
    [RequireMonitor]
    [HttpDelete("Captures/{challengeId:int}/{partId:int}/{filename}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public IActionResult DeleteTeamTraffic([FromRoute] int challengeId, [FromRoute] int partId,
        [FromRoute] string filename)
    {
        try
        {
            var file = Path.GetFileName(filename);
            var path = Path.GetFullPath(Path.Combine(FilePath.Capture, $"{challengeId}", $"{partId}", file));

            if (Path.GetExtension(file) != ".pcap" || !Path.Exists(path))
                return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_CaptureNotFound)]));

            System.IO.File.Delete(path);

            return Ok();
        }
        catch
        {
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_CaptureNotFound)]));
        }
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
    [HttpGet("{id:int}/Details")]
    [ProducesResponseType(typeof(GameDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChallengesWithTeamInfo([FromRoute] int id, CancellationToken token)
    {
        ContextInfo context = await GetContextInfo(id, token: token);

        if (context.Result is not null)
            return context.Result;

        ScoreboardModel scoreboard = await gameRepository.GetScoreboard(context.Game!, token);

        ScoreboardItem boardItem = scoreboard.Items.TryGetValue(context.Participation!.TeamId, out ScoreboardItem? item)
            ? item
            : new()
            {
                Avatar = context.Participation!.Team.AvatarUrl,
                SolvedCount = 0,
                Rank = 0,
                Name = context.Participation!.Team.Name,
                Id = context.Participation!.TeamId
            };

        return Ok(new GameDetailModel
        {
            ScoreboardItem = boardItem,
            TeamToken = context.Participation!.Token,
            Challenges = scoreboard.Challenges,
            WriteupRequired = context.Game!.WriteupRequired,
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
    [HttpGet("{id:int}/Participations")]
    [ProducesResponseType(typeof(ParticipationInfoModel[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Participations([FromRoute] int id, CancellationToken token = default)
    {
        ContextInfo context = await GetContextInfo(id, token: token);

        if (context.Game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)]));

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
    /// <param name="excelHelper"></param>
    /// <param name="token"></param>
    /// <response code="200">成功下载比赛积分榜</response>
    /// <response code="400">操作无效</response>
    /// <response code="404">比赛未找到</response>
    [RequireMonitor]
    [HttpGet("{id:int}/ScoreboardSheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public async Task<IActionResult> ScoreboardSheet([FromRoute] int id, [FromServices] ExcelHelper excelHelper,
        CancellationToken token = default)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)]));

        if (DateTimeOffset.UtcNow < game.StartTimeUtc)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted)]));

        try
        {
            ScoreboardModel scoreboard = await gameRepository.GetScoreboardWithMembers(game, token);
            MemoryStream stream = excelHelper.GetScoreboardExcel(scoreboard, game);
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{game.Title}_Scoreboard_{DateTimeOffset.Now:yyyyMMddHHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            logger.SystemLog(Program.StaticLocalizer[nameof(Resources.Program.Game_ScoreboardDownloadFailed)],
                TaskStatus.Failed, LogLevel.Error);
            logger.LogError(ex, ex.Message);
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_ScoreboardDownloadFailed)]));
        }
    }

    /// <summary>
    /// 下载比赛全部提交
    /// </summary>
    /// <remarks>
    /// 下载比赛全部提交，需要Monitor权限
    /// </remarks>
    /// <param name="id">比赛Id</param>
    /// <param name="excelHelper"></param>
    /// <param name="token"></param>
    /// <response code="200">成功下载比赛全部提交</response>
    /// <response code="400">操作无效</response>
    /// <response code="404">比赛未找到</response>
    [RequireMonitor]
    [HttpGet("{id:int}/SubmissionSheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public async Task<IActionResult> SubmissionSheet([FromRoute] int id, [FromServices] ExcelHelper excelHelper,
        CancellationToken token = default)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)]));

        if (DateTimeOffset.UtcNow < game.StartTimeUtc)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted)]));

        Submission[] submissions = await submissionRepository.GetSubmissions(game, count: 0, token: token);

        MemoryStream stream = excelHelper.GetSubmissionExcel(submissions);
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
    [HttpGet("{id:int}/Challenges/{challengeId:int}")]
    [ProducesResponseType(typeof(ChallengeDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChallenge([FromRoute] int id, [FromRoute] int challengeId,
        CancellationToken token)
    {
        if (id <= 0 || challengeId <= 0)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        ContextInfo context = await GetContextInfo(id, token: token);

        if (context.Result is not null)
            return context.Result;

        GameInstance? instance = await gameInstanceRepository.GetInstance(context.Participation!, challengeId, token);

        if (instance is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_ChallengeNotFound)],
                StatusCodes.Status404NotFound));

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
    [HttpPost("{id:int}/Challenges/{challengeId:int}")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Submit))]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submit([FromRoute] int id, [FromRoute] int challengeId,
        [FromBody] FlagSubmitModel model, CancellationToken token)
    {
        ContextInfo context = await GetContextInfo(id, challengeId, token: token);

        if (context.Result is not null)
            return context.Result;

        Submission submission = new()
        {
            Answer = model.Flag.Trim(),
            Game = context.Game!,
            User = context.User!,
            GameChallenge = context.Challenge!,
            Team = context.Participation!.Team,
            Participation = context.Participation!,
            Status = AnswerResult.FlagSubmitted,
            SubmitTimeUtc = DateTimeOffset.UtcNow
        };

        submission = await submissionRepository.AddSubmission(submission, token);

        // send to flag checker service
        await channelWriter.WriteAsync(submission, token);

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
    [HttpGet("{id:int}/Challenges/{challengeId:int}/Status/{submitId:int}")]
    [ProducesResponseType(typeof(AnswerResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Status([FromRoute] int id, [FromRoute] int challengeId, [FromRoute] int submitId,
        CancellationToken token)
    {
        var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (claimId is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_SubmissionNotFound)],
                StatusCodes.Status404NotFound));

        Submission? submission =
            await submissionRepository.GetSubmission(id, challengeId, Guid.Parse(claimId), submitId, token);

        if (submission is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Submission_NotFound)],
                StatusCodes.Status404NotFound));

        return Ok(submission.Status switch
        {
            AnswerResult.CheatDetected => AnswerResult.WrongAnswer,
            var x => x
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
    [HttpGet("{id:int}/Writeup")]
    [ProducesResponseType(typeof(BasicWriteupInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetWriteup([FromRoute] int id, CancellationToken token)
    {
        ContextInfo context = await GetContextInfo(id, denyAfterEnded: false, token: token);

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
    [HttpPost("{id:int}/Writeup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitWriteup([FromRoute] int id, IFormFile file, CancellationToken token)
    {
        switch (file.Length)
        {
            case 0:
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_SizeZero)]));
            case > 20 * 1024 * 1024:
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_SizeTooLarge)]));
        }

        if (file.ContentType != "application/pdf" || Path.GetExtension(file.FileName) != ".pdf")
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_PdfOnly)]));

        ContextInfo context = await GetContextInfo(id, denyAfterEnded: false, token: token);

        if (context.Result is not null)
            return context.Result;

        Game game = context.Game!;

        if (!game.WriteupRequired)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_WriteupNotNeeded)]));

        Participation part = context.Participation!;
        Team team = part.Team;

        if (DateTimeOffset.UtcNow > game.WriteupDeadline)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_DeadlineExpired)]));

        LocalFile? wp = context.Participation!.Writeup;

        if (wp is not null)
            await fileService.DeleteFile(wp, token);

        part.Writeup = await fileService.CreateOrUpdateFile(file,
            $"Writeup-{game.Id}-{team.Id}-{DateTimeOffset.Now:yyyyMMdd-HH.mm.ssZ}.pdf", token);

        await participationRepository.SaveAsync(token);

        logger.Log(Program.StaticLocalizer[nameof(Resources.Program.Game_WriteupSubmitted), team.Name, game.Title],
            context.User!,
            TaskStatus.Success);

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
    [HttpPost("{id:int}/Container/{challengeId:int}")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Container))]
    [ProducesResponseType(typeof(ContainerInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreateContainer([FromRoute] int id, [FromRoute] int challengeId,
        CancellationToken token)
    {
        ContextInfo context = await GetContextInfo(id, token: token);

        if (context.Result is not null)
            return context.Result;

        GameInstance? instance = await gameInstanceRepository.GetInstance(context.Participation!, challengeId, token);

        if (instance is null || !instance.Challenge.IsEnabled)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        if (!instance.Challenge.Type.IsContainer())
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerCreationNotAllowed)]));

        if (DateTimeOffset.UtcNow - instance.LastContainerOperation < TimeSpan.FromSeconds(10))
            return new JsonResult(new RequestResponse(localizer[nameof(Resources.Program.Game_OperationTooFrequent)],
                StatusCodes.Status429TooManyRequests))
            { StatusCode = StatusCodes.Status429TooManyRequests };

        if (instance.Container is not null)
        {
            if (instance.Container.Status == ContainerStatus.Running)
                return BadRequest(
                    new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerAlreadyCreated)]));

            await containerRepository.DestroyContainer(instance.Container, token);
        }

        return await gameInstanceRepository.CreateContainer(instance, context.Participation!.Team, context.User!,
                context.Game!, token) switch
        {
            null or (TaskStatus.Failed, null) => BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerCreationFailed)])),
            (TaskStatus.Denied, null) => BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerNumberLimitExceeded),
                    context.Game!.ContainerCountLimit])),
            (TaskStatus.Success, var x) => Ok(ContainerInfoModel.FromContainer(x!)),
            _ => throw new UnreachableException()
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
    [HttpPost("{id:int}/Container/{challengeId:int}/Extend")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Container))]
    [ProducesResponseType(typeof(ContainerInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExtendContainerLifetime([FromRoute] int id, [FromRoute] int challengeId,
        CancellationToken token)
    {
        ContextInfo context = await GetContextInfo(id, token: token);

        if (context.Result is not null)
            return context.Result;

        GameInstance? instance = await gameInstanceRepository.GetInstance(context.Participation!, challengeId, token);

        if (instance is null || !instance.Challenge.IsEnabled)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        if (!instance.Challenge.Type.IsContainer())
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerCreationNotAllowed)]));

        if (instance.Container is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerNotCreated)]));

        if (instance.Container.ExpectStopAt - DateTimeOffset.UtcNow >
            TimeSpan.FromMinutes(containerPolicy.Value.RenewalWindow))
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerExtensionNotAvailable)]));

        await containerRepository.ExtendLifetime(instance.Container,
            TimeSpan.FromMinutes(containerPolicy.Value.ExtensionDuration), token);

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
    [HttpDelete("{id:int}/Container/{challengeId:int}")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Container))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DeleteContainer([FromRoute] int id, [FromRoute] int challengeId,
        CancellationToken token)
    {
        ContextInfo context = await GetContextInfo(id, token: token);

        if (context.Result is not null)
            return context.Result;

        GameInstance? instance = await gameInstanceRepository.GetInstance(context.Participation!, challengeId, token);

        if (instance is null || !instance.Challenge.IsEnabled)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        if (!instance.Challenge.Type.IsContainer())
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerCreationNotAllowed)]));

        if (instance.Container is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerNotCreated)]));

        if (DateTimeOffset.UtcNow - instance.LastContainerOperation < TimeSpan.FromSeconds(10))
            return new JsonResult(new RequestResponse(localizer[nameof(Resources.Program.Game_OperationTooFrequent)],
                StatusCodes.Status429TooManyRequests))
            { StatusCode = StatusCodes.Status429TooManyRequests };

        var destroyId = instance.Container.ContainerId;

        if (!await containerRepository.DestroyContainer(instance.Container, token))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerDeletionFailed)]));

        instance.LastContainerOperation = DateTimeOffset.UtcNow;

        await gameEventRepository.AddEvent(
            new()
            {
                Type = EventType.ContainerDestroy,
                GameId = context.Game!.Id,
                TeamId = context.Participation!.TeamId,
                UserId = context.User!.Id,
                Values = [instance.Challenge.Id.ToString(), instance.Challenge.Title]
            }, token);

        logger.Log(
            Program.StaticLocalizer[nameof(Resources.Program.Game_ContainerDeleted), context.Participation!.Team.Name,
                instance.Challenge.Title,
                destroyId],
            context.User, TaskStatus.Success);

        return Ok();
    }

    async Task<ContextInfo> GetContextInfo(int id, int challengeId = 0, bool withFlag = false,
        bool denyAfterEnded = true, CancellationToken token = default)
    {
        ContextInfo res = new()
        {
            User = await userManager.GetUserAsync(User),
            Game = await gameRepository.GetGameById(id, token)
        };

        if (res.Game is null)
            return res.WithResult(NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound)));

        Participation? part = await participationRepository.GetParticipation(res.User!, res.Game, token);

        if (part is null)
            return res.WithResult(
                BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotParticipated)])));

        res.Participation = part;

        if (part.Status != ParticipationStatus.Accepted)
            return res.WithResult(
                BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_ParticipationNotAccepted)])));

        if (DateTimeOffset.UtcNow < res.Game.StartTimeUtc)
            return res.WithResult(
                BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted), ErrorCodes.GameNotStarted])));

        if (denyAfterEnded && !res.Game.PracticeMode && res.Game.EndTimeUtc < DateTimeOffset.UtcNow)
            return res.WithResult(
                    BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_Ended)], ErrorCodes.GameEnded)));

        if (challengeId <= 0)
            return res;

        GameChallenge? challenge = await challengeRepository.GetChallenge(id, challengeId, withFlag, token);

        if (challenge is null)
            return res.WithResult(NotFound(new RequestResponse(
                localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound)));

        res.Challenge = challenge;

        return res;
    }

    static Task<List<int>> GetDirNamesAsInt(string dir)
    {
        if (!Directory.Exists(dir))
            return Task.FromResult(new List<int>());

        return Task.Run(() => Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly).Select(d =>
        {
            var name = Path.GetFileName(d);
            return int.TryParse(name, out var res) ? res : -1;
        }).Where(d => d > 0).ToList());
    }

    class ContextInfo
    {
        public GameChallenge? Challenge;
        public Game? Game;
        public Participation? Participation;
        public IActionResult? Result;
        public UserInfo? User;

        public ContextInfo WithResult(IActionResult res)
        {
            Result = res;
            return this;
        }
    }
}
