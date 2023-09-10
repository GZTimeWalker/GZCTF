using System.Net.Mime;
using System.Text.RegularExpressions;
using GZCTF.Middlewares;
using GZCTF.Models.Request.Info;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GZCTF.Controllers;

/// <summary>
/// 队伍数据交互接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public partial class TeamController(UserManager<UserInfo> userManager,
    IFileRepository fileService,
    ILogger<TeamController> logger,
    ITeamRepository teamRepository,
    IParticipationRepository participationRepository) : ControllerBase
{
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
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TeamInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBasicInfo(int id, CancellationToken token)
    {
        Team? team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return NotFound(new RequestResponse("队伍不存在", StatusCodes.Status404NotFound));

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
        UserInfo? user = await userManager.GetUserAsync(User);

        return Ok((await teamRepository.GetUserTeams(user!, token)).Select(t => TeamInfoModel.FromTeam(t)));
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
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Concurrency))]
    [ProducesResponseType(typeof(TeamInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateTeam([FromBody] TeamUpdateModel model, CancellationToken token)
    {
        UserInfo? user = await userManager.GetUserAsync(User);

        Team[] teams = await teamRepository.GetUserTeams(user!, token);

        if (teams.Length > 1 && teams.Any(t => t.CaptainId == user!.Id))
            return BadRequest(new RequestResponse("不允许创建多个队伍"));

        if (string.IsNullOrEmpty(model.Name))
            return BadRequest(new RequestResponse("队伍名不能为空"));

        Team? team = await teamRepository.CreateTeam(model, user!, token);

        if (team is null)
            return BadRequest(new RequestResponse("队伍创建失败"));

        await userManager.UpdateAsync(user!);

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
    [RequireUser]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TeamInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateTeam([FromRoute] int id, [FromBody] TeamUpdateModel model,
        CancellationToken token)
    {
        UserInfo? user = await userManager.GetUserAsync(User);
        Team? team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return BadRequest(new RequestResponse("队伍未找到"));

        if (team.CaptainId != user!.Id)
            return new JsonResult(new RequestResponse("无权访问", StatusCodes.Status403Forbidden)) { StatusCode = StatusCodes.Status403Forbidden };

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
    [RequireUser]
    [HttpPut("{id:int}/Transfer")]
    [ProducesResponseType(typeof(TeamInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Transfer([FromRoute] int id, [FromBody] TeamTransferModel model,
        CancellationToken token)
    {
        UserInfo? user = await userManager.GetUserAsync(User);
        Team? team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return BadRequest(new RequestResponse("队伍未找到"));

        if (team.CaptainId != user!.Id)
            return new JsonResult(new RequestResponse("无权访问", StatusCodes.Status403Forbidden)) { StatusCode = StatusCodes.Status403Forbidden };

        if (team.Locked && await teamRepository.AnyActiveGame(team, token))
            return BadRequest(new RequestResponse("队伍已锁定"));

        UserInfo? newCaptain = await userManager.Users.SingleOrDefaultAsync(u => u.Id == model.NewCaptainId, token);

        if (newCaptain is null)
            return BadRequest(new RequestResponse("移交的用户不存在"));

        Team[] newCaptainTeams = await teamRepository.GetUserTeams(newCaptain, token);

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
    [RequireUser]
    [HttpGet("{id:int}/Invite")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> InviteCode([FromRoute] int id, CancellationToken token)
    {
        UserInfo? user = await userManager.GetUserAsync(User);
        Team? team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return BadRequest(new RequestResponse("队伍未找到"));

        if (team.CaptainId != user!.Id)
            return new JsonResult(new RequestResponse("无权访问", StatusCodes.Status403Forbidden)) { StatusCode = StatusCodes.Status403Forbidden };

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
    [RequireUser]
    [HttpPut("{id:int}/Invite")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateInviteToken([FromRoute] int id, CancellationToken token)
    {
        UserInfo? user = await userManager.GetUserAsync(User);
        Team? team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return BadRequest(new RequestResponse("队伍未找到"));

        if (team.CaptainId != user!.Id)
            return new JsonResult(new RequestResponse("无权访问", StatusCodes.Status403Forbidden)) { StatusCode = StatusCodes.Status403Forbidden };

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
    [RequireUser]
    [HttpPost("{id:int}/Kick/{userid}")]
    [ProducesResponseType(typeof(TeamInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> KickUser([FromRoute] int id, [FromRoute] string userid, CancellationToken token)
    {
        UserInfo? user = await userManager.GetUserAsync(User);
        Team? team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return BadRequest(new RequestResponse("队伍未找到"));

        if (team.CaptainId != user!.Id)
            return new JsonResult(new RequestResponse("无权访问", StatusCodes.Status403Forbidden)) { StatusCode = StatusCodes.Status403Forbidden };

        IDbContextTransaction trans = await teamRepository.BeginTransactionAsync(token);

        try
        {
            if (team.Locked && await teamRepository.AnyActiveGame(team, token))
                return BadRequest(new RequestResponse("队伍已锁定"));

            UserInfo? kickUser = team.Members.SingleOrDefault(m => m.Id == userid);
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

    [GeneratedRegex(":\\d+:[0-9a-f]{32}")]
    private static partial Regex InviteCodeRegex();

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
    [RequireUser]
    [HttpPost("Accept")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Accept([FromBody] string code, CancellationToken cancelToken)
    {
        if (!InviteCodeRegex().IsMatch(code))
            return BadRequest(new RequestResponse("Code 无效"));

        var inviteToken = code[^32..];
        var preCode = code[..^33];

        var lastColon = preCode.LastIndexOf(':');

        if (!int.TryParse(preCode[(lastColon + 1)..], out var teamId))
            return BadRequest(new RequestResponse($"队伍 Id 转换错误：{preCode[(lastColon + 1)..]}"));

        var teamName = preCode[..lastColon];
        IDbContextTransaction trans = await teamRepository.BeginTransactionAsync(cancelToken);

        try
        {
            Team? team = await teamRepository.GetTeamById(teamId, cancelToken);

            if (team is null)
                return BadRequest(new RequestResponse($"{teamName} 队伍未找到"));

            if (team.InviteToken != inviteToken)
                return BadRequest(new RequestResponse($"{teamName} 邀请无效"));

            if (team.Locked && await teamRepository.AnyActiveGame(team, cancelToken))
                return BadRequest(new RequestResponse($"{teamName} 队伍已锁定"));

            UserInfo? user = await userManager.GetUserAsync(User);

            if (team.Members.Any(m => m.Id == user!.Id))
                return BadRequest(new RequestResponse("你已经加入此队伍，无需重复加入"));

            team.Members.Add(user!);

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
    [RequireUser]
    [HttpPost("{id:int}/Leave")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Leave([FromRoute] int id, CancellationToken token)
    {
        IDbContextTransaction trans = await teamRepository.BeginTransactionAsync(token);

        try
        {
            Team? team = await teamRepository.GetTeamById(id, token);

            if (team is null)
                return NotFound(new RequestResponse("队伍未找到", StatusCodes.Status404NotFound));

            UserInfo? user = await userManager.GetUserAsync(User);

            if (team.Members.All(m => m.Id != user!.Id))
                return BadRequest(new RequestResponse("你不在此队伍中，无法离队"));

            if (team.Locked && await teamRepository.AnyActiveGame(team, token))
                return BadRequest(new RequestResponse("队伍已锁定"));

            team.Members.Remove(user!);

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
    [RequireUser]
    [HttpPut("{id:int}/Avatar")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Avatar([FromRoute] int id, IFormFile file, CancellationToken token)
    {
        UserInfo? user = await userManager.GetUserAsync(User);
        Team? team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return NotFound(new RequestResponse("队伍未找到", StatusCodes.Status404NotFound));

        if (team.CaptainId != user!.Id)
            return new JsonResult(new RequestResponse("无权访问", StatusCodes.Status403Forbidden)) { StatusCode = StatusCodes.Status403Forbidden };

        if (file.Length == 0)
            return BadRequest(new RequestResponse("文件非法"));

        if (file.Length > 3 * 1024 * 1024)
            return BadRequest(new RequestResponse("文件过大"));

        if (team.AvatarHash is not null)
            _ = await fileService.DeleteFileByHash(team.AvatarHash, token);

        LocalFile? avatar = await fileService.CreateOrUpdateImage(file, "avatar", 300, token);

        if (avatar is null)
            return BadRequest(new RequestResponse("队伍头像更新失败"));

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
    [RequireUser]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(TeamInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteTeam(int id, CancellationToken token)
    {
        UserInfo? user = await userManager.GetUserAsync(User);
        Team? team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return NotFound(new RequestResponse("队伍未找到", StatusCodes.Status404NotFound));

        if (team.CaptainId != user!.Id)
            return new JsonResult(new RequestResponse("无权访问", StatusCodes.Status403Forbidden)) { StatusCode = StatusCodes.Status403Forbidden };

        if (team.Locked && await teamRepository.AnyActiveGame(team, token))
            return BadRequest(new RequestResponse("队伍已锁定"));

        await teamRepository.DeleteTeam(team, token);

        logger.Log($"删除队伍 {team.Name}", user, TaskStatus.Success);

        return Ok();
    }
}