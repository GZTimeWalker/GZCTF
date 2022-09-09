using CTFServer.Middlewares;
using CTFServer.Models.Request.Info;
using CTFServer.Repositories.Interface;
using CTFServer.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;
using System.Text.RegularExpressions;

namespace CTFServer.Controllers;

/// <summary>
/// 队伍数据交互接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class TeamController : ControllerBase
{
    private readonly UserManager<UserInfo> userManager;
    private readonly IFileRepository FileService;
    private readonly ITeamRepository teamRepository;
    private readonly IParticipationRepository participationRepository;
    private readonly ILogger<TeamController> logger;

    public TeamController(UserManager<UserInfo> _userManager,
        IFileRepository _FileService,
        ILogger<TeamController> _logger,
        ITeamRepository _teamRepository,
        IParticipationRepository _participationRepository)
    {
        logger = _logger;
        userManager = _userManager;
        FileService = _FileService;
        teamRepository = _teamRepository;
        participationRepository = _participationRepository;
    }

    /// <summary>
    /// 获取队伍信息
    /// </summary>
    /// <remarks>
    /// 根据 id 获取一个队伍的基本信息
    /// </remarks>
    /// <param name="id">队伍id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取队伍信息</response>
    /// <response code="400">队伍不存在</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TeamInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBasicInfo(int id, CancellationToken token)
    {
        var team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return NotFound(new RequestResponse("队伍不存在", 404));

        return Ok(TeamInfoModel.FromTeam(team));
    }

    /// <summary>
    /// 获取当前自己队伍信息
    /// </summary>
    /// <remarks>
    /// 根据用户获取一个队伍的基本信息
    /// </remarks>
    /// <param name="token"></param>
    /// <response code="200">成功获取队伍信息</response>
    /// <response code="400">队伍不存在</response>
    [HttpGet]
    [RequireUser]
    [ProducesResponseType(typeof(TeamInfoModel[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTeamsInfo(CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);

        return Ok((await teamRepository.GetUserTeams(user, token))
            .Select(team => TeamInfoModel.FromTeam(team)));
    }

    /// <summary>
    /// 创建队伍
    /// </summary>
    /// <remarks>
    /// 用户创建队伍接口，每个用户只能创建一个队伍
    /// </remarks>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">成功获取队伍信息</response>
    /// <response code="400">队伍不存在</response>
    [HttpPost]
    [RequireUser]
    [ProducesResponseType(typeof(TeamInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateTeam([FromBody] TeamUpdateModel model, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);

        var teams = await teamRepository.GetUserTeams(user, token);

        if (teams.Length > 1 && teams.Any(t => t.CaptainId == user.Id))
            return BadRequest(new RequestResponse("不允许创建多个队伍"));

        if (string.IsNullOrEmpty(model.Name))
            return BadRequest(new RequestResponse("队伍名不能为空"));

        var team = await teamRepository.CreateTeam(model, user, token);

        if (team is null)
            return BadRequest(new RequestResponse("队伍创建失败"));

        await userManager.UpdateAsync(user);

        logger.Log($"创建队伍 {team.Name}", user, TaskStatus.Success);

        return Ok(TeamInfoModel.FromTeam(team));
    }

    /// <summary>
    /// 更改队伍信息
    /// </summary>
    /// <remarks>
    /// 队伍信息更改接口，需要为队伍创建者
    /// </remarks>
    /// <param name="id">队伍Id</param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">成功获取队伍信息</response>
    /// <response code="400">队伍不存在</response>
    /// <response code="401">未授权</response>
    /// <response code="403">无权操作</response>
    [HttpPut("{id}")]
    [RequireUser]
    [ProducesResponseType(typeof(TeamInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateTeam([FromRoute] int id, [FromBody] TeamUpdateModel model, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        var team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return BadRequest(new RequestResponse("队伍未找到"));

        if (team.CaptainId != user.Id)
            return new JsonResult(new RequestResponse("无权访问", 403)) { StatusCode = 403 };

        team.UpdateInfo(model);

        await teamRepository.SaveAsync(token);

        return Ok(TeamInfoModel.FromTeam(team));
    }

    /// <summary>
    /// 移交队伍所有权
    /// </summary>
    /// <remarks>
    /// 移交队伍所有权接口，需要为队伍创建者
    /// </remarks>
    /// <param name="id">队伍Id</param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">成功获取队伍信息</response>
    /// <response code="400">队伍不存在</response>
    /// <response code="401">未授权</response>
    /// <response code="403">无权操作</response>
    [HttpPut("{id}/Transfer")]
    [RequireUser]
    [ProducesResponseType(typeof(TeamInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Transfer([FromRoute] int id, [FromBody] TeamTransferModel model ,CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        var team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return BadRequest(new RequestResponse("队伍未找到"));

        if (team.CaptainId != user.Id)
            return new JsonResult(new RequestResponse("无权访问", 403)) { StatusCode = 403 };

        if (team.Locked && await teamRepository.AnyActiveGame(team, token))
            return BadRequest(new RequestResponse("队伍已锁定"));

        var newCaptain = await userManager.Users.SingleOrDefaultAsync(u => u.Id == model.NewCaptainId);

        if (newCaptain is null)
            return BadRequest(new RequestResponse("移交的用户不存在"));

        var newCaptainTeams = await teamRepository.GetUserTeams(newCaptain, token);

        if (newCaptainTeams.Count(t => t.CaptainId == newCaptain.Id) >= 3)
            return BadRequest(new RequestResponse("被移交者所管理的队伍过多"));

        await teamRepository.Transfer(team, newCaptain, token);

        return Ok(TeamInfoModel.FromTeam(team));
    }

    /// <summary>
    /// 获取邀请信息
    /// </summary>
    /// <remarks>
    /// 获取队伍邀请信息，需要为队伍创建者
    /// </remarks>
    /// <param name="id">队伍Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取队伍Token</response>
    /// <response code="400">队伍不存在</response>
    /// <response code="401">未授权</response>
    /// <response code="403">无权操作</response>
    [HttpGet("{id}/Invite")]
    [RequireUser]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> InviteCode([FromRoute] int id, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        var team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return BadRequest(new RequestResponse("队伍未找到"));

        if (team.CaptainId != user.Id)
            return new JsonResult(new RequestResponse("无权访问", 403)) { StatusCode = 403 };

        return Ok(team.InviteCode);
    }

    /// <summary>
    /// 更新邀请 Token
    /// </summary>
    /// <remarks>
    /// 更新邀请 Token 的接口，需要为队伍创建者
    /// </remarks>
    /// <param name="id">队伍Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取队伍Token</response>
    /// <response code="400">队伍不存在</response>
    /// <response code="401">未授权</response>
    /// <response code="403">无权操作</response>
    [HttpPut("{id}/Invite")]
    [RequireUser]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateInviteToken([FromRoute] int id, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        var team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return BadRequest(new RequestResponse("队伍未找到"));

        if (team.CaptainId != user.Id)
            return new JsonResult(new RequestResponse("无权访问", 403)) { StatusCode = 403 };

        team.UpdateInviteToken();

        await teamRepository.SaveAsync(token);

        return Ok(team.InviteCode);
    }

    /// <summary>
    /// 踢除用户接口
    /// </summary>
    /// <remarks>
    /// 踢除用户接口，踢出对应id的用户，需要队伍创建者权限
    /// </remarks>
    /// <param name="id">队伍Id</param>
    /// <param name="userid">被踢除用户Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取队伍Token</response>
    /// <response code="400">队伍不存在</response>
    /// <response code="401">未授权</response>
    /// <response code="403">无权操作</response>
    [HttpPost("{id}/Kick/{userid}")]
    [RequireUser]
    [ProducesResponseType(typeof(TeamInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> KickUser([FromRoute] int id, [FromRoute] string userid, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        var team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return BadRequest(new RequestResponse("队伍未找到"));

        if (team.CaptainId != user.Id)
            return new JsonResult(new RequestResponse("无权访问", 403)) { StatusCode = 403 };

        var trans = await teamRepository.BeginTransactionAsync(token);

        try
        {
            if (team is null)
                return BadRequest(new RequestResponse("队伍未找到"));

            if (team.Locked && await teamRepository.AnyActiveGame(team, token))
                return BadRequest(new RequestResponse("队伍已锁定"));

            var kickUser = team.Members.SingleOrDefault(m => m.Id == userid);
            if (kickUser is null)
                return BadRequest(new RequestResponse("用户不在队伍中"));

            team.Members.Remove(kickUser);
            await participationRepository.RemoveUserParticipations(user, team, token);

            await teamRepository.SaveAsync(token);
            await trans.CommitAsync(token);

            logger.Log($"从队伍 {team.Name} 踢除 {kickUser.UserName}", user, TaskStatus.Success);
            return Ok(TeamInfoModel.FromTeam(team));
        }
        catch
        {
            await trans.RollbackAsync(token);
            throw;
        }
    }

    /// <summary>
    /// 接受邀请
    /// </summary>
    /// <remarks>
    /// 接受邀请的接口，需要User权限，且不在队伍中
    /// </remarks>
    /// <param name="code">队伍邀请Token</param>
    /// <param name="cancelToken"></param>
    /// <response code="200">接受队伍邀请</response>
    /// <response code="400">队伍不存在</response>
    /// <response code="401">未授权</response>
    /// <response code="403">无权操作</response>
    [HttpPost("Accept")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Accept([FromBody] string code, CancellationToken cancelToken)
    {
        if (!Regex.IsMatch(code, @":\d+:[0-9a-f]{32}"))
            return BadRequest(new RequestResponse($"Code 无效"));

        var inviteCode = code[^32..];
        var preCode = code[..^33];

        var lastColon = preCode.LastIndexOf(':');
        var teamId = int.Parse(preCode[(lastColon + 1)..]);
        var teamName = preCode[..lastColon];

        var trans = await teamRepository.BeginTransactionAsync(cancelToken);

        try
        {
            var team = await teamRepository.GetTeamById(teamId, cancelToken);

            if (team is null)
                return BadRequest(new RequestResponse($"{teamName} 队伍未找到"));

            if (team.InviteCode != code)
                return BadRequest(new RequestResponse($"{teamName} 邀请无效"));

            if (team.Locked && await teamRepository.AnyActiveGame(team, cancelToken))
                return BadRequest(new RequestResponse($"{teamName} 队伍已锁定"));

            var user = await userManager.GetUserAsync(User);

            if (team.Members.Any(m => m.Id == user.Id))
                return BadRequest(new RequestResponse("你已经加入此队伍，无需重复加入"));

            team.Members.Add(user);

            await teamRepository.SaveAsync(cancelToken);
            await trans.CommitAsync(cancelToken);

            logger.Log($"加入队伍 {team.Name}", user, TaskStatus.Success);
            return Ok();
        }
        catch
        {
            await trans.RollbackAsync(cancelToken);
            throw;
        }
    }

    /// <summary>
    /// 离开队伍
    /// </summary>
    /// <remarks>
    /// 离开队伍的接口，需要User权限，且在队伍中
    /// </remarks>
    /// <param name="id">队伍Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功离开队伍</response>
    /// <response code="400">队伍不存在</response>
    /// <response code="401">未授权</response>
    /// <response code="403">无权操作</response>
    [HttpPost("{id}/Leave")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Leave([FromRoute] int id, CancellationToken token)
    {
        var trans = await teamRepository.BeginTransactionAsync(token);

        try
        {
            var team = await teamRepository.GetTeamById(id, token);

            if (team is null)
                return BadRequest(new RequestResponse("队伍未找到"));

            var user = await userManager.GetUserAsync(User);

            if (!team.Members.Any(m => m.Id == user.Id))
                return BadRequest(new RequestResponse("你不在此队伍中，无法离队"));

            if (team.Locked && await teamRepository.AnyActiveGame(team, token))
                return BadRequest(new RequestResponse("队伍已锁定"));

            team.Members.Remove(user);

            await userManager.UpdateAsync(user);
            await teamRepository.SaveAsync(token);
            await trans.CommitAsync(token);

            logger.Log($"离开队伍 {team.Name}", user, TaskStatus.Success);
            return Ok();
        }
        catch
        {
            await trans.RollbackAsync(token);
            throw;
        }
    }

    /// <summary>
    /// 更新队伍头像接口
    /// </summary>
    /// <remarks>
    /// 使用此接口更新队伍头像，需要User权限，且为队伍成员
    /// </remarks>
    /// <response code="200">用户头像URL</response>
    /// <response code="400">非法请求</response>
    /// <response code="401">未授权用户</response>
    [HttpPut("{id}/Avatar")]
    [RequireUser]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Avatar([FromRoute] int id, IFormFile file, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        var team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return BadRequest(new RequestResponse("队伍未找到"));

        if (team.CaptainId != user.Id)
            return new JsonResult(new RequestResponse("无权访问", 403)) { StatusCode = 403 };

        if (file.Length == 0)
            return BadRequest(new RequestResponse("文件非法"));

        if (file.Length > 3 * 1024 * 1024)
            return BadRequest(new RequestResponse("文件过大"));

        if (team.AvatarHash is not null)
            _ = await FileService.DeleteFileByHash(team.AvatarHash, token);

        var avatar = await FileService.CreateOrUpdateFile(file, "avatar", token);

        if (avatar is null)
            return BadRequest(new RequestResponse("未知错误"));

        team.AvatarHash = avatar.Hash;
        await teamRepository.SaveAsync(token);

        logger.Log($"队伍 {team.Name} 更改新头像：[{avatar.Hash[..8]}]", user, TaskStatus.Success);

        return Ok(avatar.Url());
    }

    /// <summary>
    /// 删除队伍
    /// </summary>
    /// <remarks>
    /// 用户删除队伍接口，需要User权限，且为队伍队长
    /// </remarks>
    /// <param name="id">队伍Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取队伍信息</response>
    /// <response code="400">队伍不存在</response>
    [HttpDelete("{id}")]
    [RequireUser]
    [ProducesResponseType(typeof(TeamInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteTeam(int id, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        var team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return BadRequest(new RequestResponse("队伍未找到"));

        if (team.CaptainId != user.Id)
            return new JsonResult(new RequestResponse("无权访问", 403)) { StatusCode = 403 };

        if (team.Locked && await teamRepository.AnyActiveGame(team, token))
            return BadRequest(new RequestResponse("队伍已锁定"));

        await teamRepository.DeleteTeam(team!, token);

        logger.Log($"删除队伍 {team!.Name}", user, TaskStatus.Success);

        return Ok();
    }
}