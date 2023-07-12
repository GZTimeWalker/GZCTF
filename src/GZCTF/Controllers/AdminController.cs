using System.Net.Mime;
using GZCTF.Extensions;
using GZCTF.Middlewares;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Admin;
using GZCTF.Models.Request.Info;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Interface;
using GZCTF.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GZCTF.Controllers;

/// <summary>
/// 管理员数据交互接口
/// </summary>
[RequireAdmin]
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class AdminController : ControllerBase
{
    private readonly ILogger<AdminController> logger;
    private readonly UserManager<UserInfo> userManager;
    private readonly ILogRepository logRepository;
    private readonly IFileRepository fileService;
    private readonly IConfigService configService;
    private readonly IGameRepository gameRepository;
    private readonly ITeamRepository teamRepository;
    private readonly IServiceProvider serviceProvider;
    private readonly IContainerRepository containerRepository;
    private readonly IInstanceRepository instanceRepository;
    private readonly IParticipationRepository participationRepository;
    private readonly string basepath;

    public AdminController(UserManager<UserInfo> _userManager,
        ILogger<AdminController> _logger,
        IFileRepository _FileService,
        ILogRepository _logRepository,
        IConfigService _configService,
        IGameRepository _gameRepository,
        ITeamRepository _teamRepository,
        IInstanceRepository _instanceRepository,
        IContainerRepository _containerRepository,
        IServiceProvider _serviceProvider,
        IConfiguration _configuration,
        IParticipationRepository _participationRepository)
    {
        logger = _logger;
        userManager = _userManager;
        fileService = _FileService;
        configService = _configService;
        logRepository = _logRepository;
        teamRepository = _teamRepository;
        gameRepository = _gameRepository;
        instanceRepository = _instanceRepository;
        containerRepository = _containerRepository;
        serviceProvider = _serviceProvider;
        participationRepository = _participationRepository;
        basepath = _configuration.GetSection("UploadFolder").Value ?? "uploads";
    }

    /// <summary>
    /// 获取配置
    /// </summary>
    /// <remarks>
    /// 使用此接口获取全局设置，需要Admin权限
    /// </remarks>
    /// <response code="200">全局配置</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">禁止访问</response>
    [HttpGet("Config")]
    [ProducesResponseType(typeof(ConfigEditModel), StatusCodes.Status200OK)]
    public IActionResult GetConfigs()
    {
        // always reload, ensure latest
        configService.ReloadConfig();

        ConfigEditModel config = new()
        {
            AccountPolicy = serviceProvider.GetRequiredService<IOptionsSnapshot<AccountPolicy>>().Value,
            GlobalConfig = serviceProvider.GetRequiredService<IOptionsSnapshot<GlobalConfig>>().Value,
            GamePolicy = serviceProvider.GetRequiredService<IOptionsSnapshot<GamePolicy>>().Value
        };

        return Ok(config);
    }

    /// <summary>
    /// 更改配置
    /// </summary>
    /// <remarks>
    /// 使用此接口更改全局设置，需要Admin权限
    /// </remarks>
    /// <response code="200">更新成功</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">禁止访问</response>
    [HttpPut("Config")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateConfigs([FromBody] ConfigEditModel model, CancellationToken token)
    {
        foreach (var prop in typeof(ConfigEditModel).GetProperties())
        {
            var value = prop.GetValue(model);
            if (value is not null)
                await configService.SaveConfig(prop.PropertyType, value, token);
        }

        return Ok();
    }

    /// <summary>
    /// 获取全部用户
    /// </summary>
    /// <remarks>
    /// 使用此接口获取全部用户，需要Admin权限
    /// </remarks>
    /// <response code="200">用户列表</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">禁止访问</response>
    [HttpGet("Users")]
    [ProducesResponseType(typeof(ArrayResponse<UserInfoModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Users([FromQuery] int count = 100, [FromQuery] int skip = 0, CancellationToken token = default)
        => Ok((await (
            from user in userManager.Users.OrderBy(e => e.Id).Skip(skip).Take(count)
            select UserInfoModel.FromUserInfo(user)
           ).ToArrayAsync(token)).ToResponse(await userManager.Users.CountAsync(token)));

    /// <summary>
    /// 批量添加用户
    /// </summary>
    /// <remarks>
    /// 使用此接口批量添加用户，需要Admin权限
    /// </remarks>
    /// <response code="200">成功添加</response>
    /// <response code="400">用户校验失败</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">禁止访问</response>
    [HttpPost("Users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddUsers([FromBody] UserCreateModel[] model, CancellationToken token = default)
    {
        var currentUser = await userManager.GetUserAsync(User);
        var trans = await teamRepository.BeginTransactionAsync(token);

        try
        {
            var users = new List<(UserInfo, string?)>(model.Length);
            foreach (var user in model)
            {
                var userInfo = user.ToUserInfo();
                var result = await userManager.CreateAsync(userInfo, user.Password);

                if (!result.Succeeded)
                {
                    switch (result.Errors.FirstOrDefault()?.Code)
                    {
                        case "DuplicateEmail":
                            userInfo = await userManager.FindByEmailAsync(user.Email);
                            break;
                        case "DuplicateUserName":
                            userInfo = await userManager.FindByNameAsync(user.UserName);
                            break;
                        default:
                            await trans.RollbackAsync(token);
                            return BadRequest(new RequestResponse(result.Errors.FirstOrDefault()?.Description ?? "未知错误"));
                    }

                    if (userInfo is not null)
                    {
                        userInfo.UpdateUserInfo(user);
                        var code = await userManager.GeneratePasswordResetTokenAsync(userInfo);
                        result = await userManager.ResetPasswordAsync(userInfo, code, user.Password);
                    }

                    if (!result.Succeeded || userInfo is null)
                    {
                        await trans.RollbackAsync(token);
                        return BadRequest(new RequestResponse(result.Errors.FirstOrDefault()?.Description ?? "未知错误"));
                    }
                }

                users.Add((userInfo, user.TeamName));
            }

            var teams = new List<Team>();
            foreach (var (user, teamName) in users)
            {
                if (teamName is null)
                    continue;

                var team = teams.Find(team => team.Name == teamName);
                if (team is null)
                {
                    team = await teamRepository.CreateTeam(new(teamName), user, token);
                    teams.Add(team!);
                }
                else
                {
                    team.Members.Add(user);
                }
            }

            await teamRepository.SaveAsync(token);
            await trans.CommitAsync(token);

            logger.Log($"成功批量添加 {users.Count} 个用户", currentUser, TaskStatus.Success);

            return Ok();
        }
        catch
        {
            await trans.RollbackAsync(token);
            throw;
        }
    }

    /// <summary>
    /// 搜索用户
    /// </summary>
    /// <remarks>
    /// 使用此接口搜索用户，需要Admin权限
    /// </remarks>
    /// <response code="200">用户列表</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">禁止访问</response>
    [HttpPost("Users/Search")]
    [ProducesResponseType(typeof(ArrayResponse<UserInfoModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchUsers([FromQuery] string hint, CancellationToken token = default)
        => Ok((await (
            from user in userManager.Users
                .Where(item =>
                    EF.Functions.Like(item.UserName!, $"%{hint}%") ||
                    EF.Functions.Like(item.StdNumber, $"%{hint}%") ||
                    EF.Functions.Like(item.Email!, $"%{hint}%") ||
                    EF.Functions.Like(item.Id, $"%{hint}%") ||
                    EF.Functions.Like(item.RealName, $"%{hint}%")
                )
                .OrderBy(e => e.Id).Take(30)
            select UserInfoModel.FromUserInfo(user)
           ).ToArrayAsync(token)).ToResponse());

    /// <summary>
    /// 获取全部队伍信息
    /// </summary>
    /// <remarks>
    /// 使用此接口获取全部队伍，需要Admin权限
    /// </remarks>
    /// <response code="200">用户列表</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">禁止访问</response>
    [HttpGet("Teams")]
    [ProducesResponseType(typeof(ArrayResponse<TeamInfoModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Teams([FromQuery] int count = 100, [FromQuery] int skip = 0, CancellationToken token = default)
        => Ok((await teamRepository.GetTeams(count, skip, token))
                .Select(team => TeamInfoModel.FromTeam(team))
                .ToResponse(await teamRepository.CountAsync(token)));

    /// <summary>
    /// 搜索队伍
    /// </summary>
    /// <remarks>
    /// 使用此接口搜索队伍，需要Admin权限
    /// </remarks>
    /// <response code="200">用户列表</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">禁止访问</response>
    [HttpPost("Teams/Search")]
    [ProducesResponseType(typeof(ArrayResponse<TeamInfoModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchTeams([FromQuery] string hint, CancellationToken token = default)
        => Ok((await teamRepository.SearchTeams(hint, token))
                .Select(team => TeamInfoModel.FromTeam(team))
                .ToResponse());

    /// <summary>
    /// 修改队伍信息
    /// </summary>
    /// <remarks>
    /// 使用此接口修改队伍信息，需要Admin权限
    /// </remarks>
    /// <response code="200">成功更新</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">禁止访问</response>
    /// <response code="404">队伍未找到</response>
    [HttpPut("Teams/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTeam([FromRoute] int id, [FromBody] AdminTeamModel model, CancellationToken token = default)
    {
        var team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return BadRequest(new RequestResponse("队伍未找到"));

        team.UpdateInfo(model);
        await teamRepository.SaveAsync(token);

        return Ok();
    }

    /// <summary>
    /// 修改用户信息
    /// </summary>
    /// <remarks>
    /// 使用此接口修改用户信息，需要Admin权限
    /// </remarks>
    /// <response code="200">成功更新</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">禁止访问</response>
    /// <response code="404">用户未找到</response>
    [HttpPut("Users/{userid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserInfo(string userid, [FromBody] AdminUserInfoModel model)
    {
        var user = await userManager.FindByIdAsync(userid);

        if (user is null)
            return NotFound(new RequestResponse("用户未找到", 404));

        user.UpdateUserInfo(model);
        await userManager.UpdateAsync(user);

        return Ok();
    }

    /// <summary>
    /// 重置用户密码
    /// </summary>
    /// <remarks>
    /// 使用此接口重置用户密码，需要Admin权限
    /// </remarks>
    /// <response code="200">成功获取</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">禁止访问</response>
    /// <response code="404">用户未找到</response>
    [HttpDelete("Users/{userid}/Password")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(string userid, CancellationToken token = default)
    {
        var user = await userManager.FindByIdAsync(userid);

        if (user is null)
            return NotFound(new RequestResponse("用户未找到", 404));

        var pwd = Codec.RandomPassword(16);
        var code = await userManager.GeneratePasswordResetTokenAsync(user);
        await userManager.ResetPasswordAsync(user, code, pwd);

        return Ok(pwd);
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    /// <remarks>
    /// 使用此接口删除用户，需要Admin权限
    /// </remarks>
    /// <response code="200">成功获取</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">禁止访问</response>
    /// <response code="404">用户未找到</response>
    [HttpDelete("Users/{userid}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(string userid, CancellationToken token = default)
    {
        var user = await userManager.GetUserAsync(User);

        if (user!.Id == userid)
            return BadRequest(new RequestResponse("不可以删除自己"));

        user = await userManager.FindByIdAsync(userid);

        if (user is null)
            return NotFound(new RequestResponse("用户未找到", 404));

        if (await teamRepository.CheckIsCaptain(user, token))
            return BadRequest(new RequestResponse("不可以删除队长"));

        await userManager.DeleteAsync(user);

        return Ok();
    }

    /// <summary>
    /// 删除队伍
    /// </summary>
    /// <remarks>
    /// 使用此接口删除队伍，需要Admin权限
    /// </remarks>
    /// <response code="200">成功获取</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">禁止访问</response>
    /// <response code="404">用户未找到</response>
    [HttpDelete("Teams/{id}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTeam(int id, CancellationToken token = default)
    {
        var team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return NotFound(new RequestResponse("队伍未找到", 404));

        await teamRepository.DeleteTeam(team, token);

        return Ok();
    }

    /// <summary>
    /// 获取用户信息
    /// </summary>
    /// <remarks>
    /// 使用此接口获取用户信息，需要Admin权限
    /// </remarks>
    /// <response code="200">用户对象</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">禁止访问</response>
    [HttpGet("Users/{userid}")]
    [ProducesResponseType(typeof(ProfileUserInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UserInfo(string userid)
    {
        var user = await userManager.FindByIdAsync(userid);

        if (user is null)
            return NotFound(new RequestResponse("用户未找到", 404));

        return Ok(ProfileUserInfoModel.FromUserInfo(user));
    }

    /// <summary>
    /// 获取全部日志
    /// </summary>
    /// <remarks>
    /// 使用此接口获取全部日志，需要Admin权限
    /// </remarks>
    /// <response code="200">日志列表</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">禁止访问</response>
    [HttpGet("Logs")]
    [ProducesResponseType(typeof(LogMessageModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logs([FromQuery] string? level = "All", [FromQuery] int count = 50, [FromQuery] int skip = 0, CancellationToken token = default)
        => Ok(await logRepository.GetLogs(skip, count, level, token));

    /// <summary>
    /// 更新参与状态
    /// </summary>
    /// <remarks>
    /// 使用此接口更新队伍参与状态，审核申请，需要Admin权限
    /// </remarks>
    /// <response code="200">更新成功</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">禁止访问</response>
    /// <response code="404">参与对象未找到</response>
    [HttpPut("Participation/{id}/{status}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Participation(int id, ParticipationStatus status, CancellationToken token = default)
    {
        var participation = await participationRepository.GetParticipationById(id, token);

        if (participation is null)
            return NotFound(new RequestResponse("参与状态未找到", 404));

        await participationRepository.UpdateParticipationStatus(participation, status, token);

        return Ok();
    }

    /// <summary>
    /// 获取全部 Writeup 基本信息
    /// </summary>
    /// <remarks>
    /// 使用此接口获取 Writeup 基本信息，需要Admin权限
    /// </remarks>
    /// <response code="200">更新成功</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">禁止访问</response>
    /// <response code="404">比赛未找到</response>
    [HttpGet("Writeups/{id}")]
    [ProducesResponseType(typeof(WriteupInfoModel[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Writeups(int id, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        return Ok(await participationRepository.GetWriteups(game, token));
    }

    /// <summary>
    /// 下载全部 Writeup
    /// </summary>
    /// <remarks>
    /// 使用此接口下载全部 Writeup，需要Admin权限
    /// </remarks>
    /// <response code="200">下载成功</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">禁止访问</response>
    /// <response code="404">比赛未找到</response>
    [HttpGet("Writeups/{id}/All")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAllWriteups(int id, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse("比赛未找到", 404));

        var wps = await participationRepository.GetWriteups(game, token);
        var filename = $"Writeups-{game.Title}-{DateTimeOffset.UtcNow:yyyyMMdd-HH.mm.ssZ}";
        var stream = await Codec.ZipFilesAsync(wps.Select(p => p.File), basepath, filename, token);
        stream.Seek(0, SeekOrigin.Begin);

        return File(stream, "application/zip", $"{filename}.zip");
    }

    /// <summary>
    /// 获取全部容器实例
    /// </summary>
    /// <remarks>
    /// 使用此接口获取全部容器实例，需要Admin权限
    /// </remarks>
    /// <response code="200">实例列表</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">禁止访问</response>
    [HttpGet("Instances")]
    [ProducesResponseType(typeof(ArrayResponse<ContainerInstanceModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Instances(CancellationToken token = default)
        => Ok(new ArrayResponse<ContainerInstanceModel>(await containerRepository.GetContainerInstances(token)));

    /// <summary>
    /// 删除容器实例
    /// </summary>
    /// <remarks>
    /// 使用此接口强制删除容器实例，需要Admin权限
    /// </remarks>
    /// <response code="200">成功获取</response>
    /// <response code="400">容器实例销毁失败</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">禁止访问</response>
    /// <response code="404">容器实例未找到</response>
    [HttpDelete("Instances/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DestroyInstance(string id, CancellationToken token = default)
    {
        var container = await containerRepository.GetContainerById(id, token);

        if (container is null)
            return NotFound(new RequestResponse("容器实例未找到", 404));

        if (await instanceRepository.DestroyContainer(container, token))
            return Ok();
        else
            return BadRequest(new RequestResponse("容器实例销毁失败", 404));
    }

    /// <summary>
    /// 获取全部文件
    /// </summary>
    /// <remarks>
    /// 使用此接口获取全部文件，需要Admin权限
    /// </remarks>
    /// <response code="200">文件列表</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">禁止访问</response>
    [HttpGet("Files")]
    [ProducesResponseType(typeof(ArrayResponse<LocalFile>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Files([FromQuery] int count = 50, [FromQuery] int skip = 0, CancellationToken token = default)
        => Ok(new ArrayResponse<LocalFile>(await fileService.GetFiles(count, skip, token)));
}
