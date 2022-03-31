using Microsoft.AspNetCore.Mvc;
using CTFServer.Middlewares;
using CTFServer.Models;
using CTFServer.Models.Request.Teams;
using CTFServer.Repositories.Interface;
using CTFServer.Utils;
using NLog;
using System.Net.Mime;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Controllers;

/// <summary>
/// 管理员数据交互接口
/// </summary>
[RequireAdmin]
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class TeamController : ControllerBase
{
    private static readonly Logger logger = LogManager.GetLogger("TeamController");
    private readonly UserManager<UserInfo> userManager;
    private readonly ILogRepository logRepository;
    private readonly IFileRepository fileRepository;
    private readonly ITeamRepository teamRepository;

    public TeamController(UserManager<UserInfo> _userManager,
        ILogRepository _logRepository,
        IFileRepository _fileRepository,
        ITeamRepository _teamRepository)
    {
        userManager = _userManager;
        logRepository = _logRepository;
        fileRepository = _fileRepository;
        teamRepository = _teamRepository;
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
    /// 创建队伍
    /// </summary>
    /// <remarks>
    /// 用户创建队伍接口
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

        if (user.OwnTeamId is not null)
            return BadRequest(new RequestResponse("不允许创建多个队伍"));

        if (model.Name is null)
            return BadRequest(new RequestResponse("队伍名不能为空"));

        var team = await teamRepository.CreateTeam(model, user, token);

        if(team is null)
            return BadRequest(new RequestResponse("队伍创建失败"));

        user.OwnTeam = team;
        user.ActiveTeam = team;
        await userManager.UpdateAsync(user);

        return Ok(TeamInfoModel.FromTeam(team));
    }

    /// <summary>
    /// 更改队伍信息
    /// </summary>
    /// <remarks>
    /// 队伍信息更改接口
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

        if (user.OwnTeamId != id)
            return new JsonResult(new RequestResponse("无权访问", 403)) { StatusCode = 403 };

        var team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return BadRequest(new RequestResponse("队伍未找到"));

        team.UpdateInfo(model);

        await teamRepository.UpdateAsync(team, token);

        return Ok(TeamInfoModel.FromTeam(team));
    }

    /// <summary>
    /// 获取邀请信息
    /// </summary>
    /// <remarks>
    /// 获取队伍邀请信息
    /// </remarks>
    /// <param name="id">队伍Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取队伍Token</response>
    /// <response code="400">队伍不存在</response>
    /// <response code="401">未授权</response>
    /// <response code="403">无权操作</response>
    [HttpPost("{id}/Invite")]
    [RequireUser]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> TeamInviteToken([FromRoute] int id, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);

        if (user.OwnTeamId != id)
            return new JsonResult(new RequestResponse("无权访问", 403)) { StatusCode = 403 };

        var team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return BadRequest(new RequestResponse("队伍未找到"));

        return Ok(team.InviteToken);
    }

    /// <summary>
    /// 更新邀请 Token
    /// </summary>
    /// <remarks>
    /// 更新邀请 Token 的接口
    /// </remarks>
    /// <param name="id">队伍Id</param>
    /// <param name="token"></param>
    /// <response code="200">成功获取队伍Token</response>
    /// <response code="400">队伍不存在</response>
    /// <response code="401">未授权</response>
    /// <response code="403">无权操作</response>
    [HttpPost("{id}/UpdateInviteToken")]
    [RequireUser]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateInviteToken([FromRoute] int id, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);

        if (user.OwnTeamId != id)
            return new JsonResult(new RequestResponse("无权访问", 403)) { StatusCode = 403 };

        var team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return BadRequest(new RequestResponse("队伍未找到"));

        team.UpdateInviteToken();

        await teamRepository.UpdateAsync(team, token);

        return Ok(team.InviteToken);
    }

    /// <summary>
    /// 接受邀请
    /// </summary>
    /// <remarks>
    /// 接受邀请的接口，需要SignedIn权限，且不在队伍中
    /// </remarks>
    /// <param name="id">队伍Id</param>
    /// <param name="token"></param>
    /// <response code="200">接受队伍邀请</response>
    /// <response code="400">队伍不存在</response>
    /// <response code="401">未授权</response>
    /// <response code="403">无权操作</response>
    [HttpPost("{id}/Accept")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Accept([FromRoute] int id, [FromQuery] string token, CancellationToken cancelToken)
    {
        var team = await teamRepository.GetTeamById(id, cancelToken);

        if (team is null)
            return BadRequest(new RequestResponse("队伍未找到"));

        if(team.InviteToken != token)
            return BadRequest(new RequestResponse("邀请无效"));

        var user = await userManager.GetUserAsync(User);

        if (team.Members.Any(m => m.Id == user.Id))
            return BadRequest(new RequestResponse("你已经加入此队伍，无需重复加入"));

        team.Members.Add(user);

        await teamRepository.UpdateAsync(team, cancelToken);

        return Ok();
    }

    /// <summary>
    /// 更新队伍头像接口
    /// </summary>
    /// <remarks>
    /// 使用此接口更新队伍头像，需要SignedIn权限，且为队伍成员
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
    public async Task<IActionResult> Avatar([FromRoute] int id,[FromBody] IFormFile file, CancellationToken token)
    {
        var team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return BadRequest(new RequestResponse("队伍未找到"));

        var user = await userManager.GetUserAsync(User);

        if (!team.Members.Any(m => m.Id == user.Id))
            return new JsonResult(new RequestResponse("无权操作", 403)) { StatusCode = 403 };

        if (file.Length == 0)
            return BadRequest(new RequestResponse("文件非法"));

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new RequestResponse("文件过大"));

        if (user.AvatarHash is not null)
            _ = await fileRepository.DeleteFileByHash(user.AvatarHash, token);

        var avatar = await fileRepository.CreateOrUpdateFile(file, "avatar", token);

        if (avatar is null)
            return BadRequest(new RequestResponse("未知错误"));

        team.AvatarHash = avatar.Hash;
        await teamRepository.UpdateAsync(team, token);

        LogHelper.Log(logger, $"队伍 {team.Name} 更改新头像：[{avatar.Hash[..8]}]", user, TaskStatus.Success);

        return Ok(avatar.Url);
    }
}