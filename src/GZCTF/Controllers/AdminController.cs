using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Reflection;
using GZCTF.Extensions;
using GZCTF.Middlewares;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Admin;
using GZCTF.Models.Request.Info;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Localization;
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
public class AdminController(
    UserManager<UserInfo> userManager,
    ILogger<AdminController> logger,
    IFileRepository fileService,
    ILogRepository logRepository,
    IConfigService configService,
    IGameRepository gameRepository,
    ITeamRepository teamRepository,
    IContainerRepository containerRepository,
    IServiceProvider serviceProvider,
    IParticipationRepository participationRepository,
    IStringLocalizer<Program> localizer) : ControllerBase
{
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
            ContainerPolicy = serviceProvider.GetRequiredService<IOptionsSnapshot<ContainerPolicy>>().Value
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
        foreach (PropertyInfo prop in typeof(ConfigEditModel).GetProperties())
        {
            var value = prop.GetValue(model);

            if (value is null)
                continue;

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
    public async Task<IActionResult> Users([FromQuery] int count = 100, [FromQuery] int skip = 0,
        CancellationToken token = default) =>
        Ok((await (
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
        UserInfo? currentUser = await userManager.GetUserAsync(User);
        IDbContextTransaction trans = await teamRepository.BeginTransactionAsync(token);

        try
        {
            var users = new List<(UserInfo, string?)>(model.Length);
            foreach (UserCreateModel user in model)
            {
                var userInfo = user.ToUserInfo();
                IdentityResult result = await userManager.CreateAsync(userInfo, user.Password);

                if (result.Succeeded)
                    continue;

                userInfo = result.Errors.FirstOrDefault()?.Code switch
                {
                    "DuplicateEmail" => await userManager.FindByEmailAsync(user.Email),
                    "DuplicateUserName" => await userManager.FindByNameAsync(user.UserName),
                    _ => null
                };

                if (userInfo is null)
                {
                    await trans.RollbackAsync(token);
                    return HandleIdentityError(result.Errors);
                }

                userInfo.UpdateUserInfo(user);
                var code = await userManager.GeneratePasswordResetTokenAsync(userInfo);
                await userManager.ResetPasswordAsync(userInfo, code, user.Password);

                users.Add((userInfo, user.TeamName));
            }

            var teams = new List<Team>();
            foreach ((UserInfo user, var teamName) in users)
            {
                if (teamName is null)
                    continue;

                Team? team = teams.Find(team => team.Name == teamName);
                if (team is null)
                {
                    team = await teamRepository.CreateTeam(new() { Name = teamName }, user, token);
                    teams.Add(team);
                }
                else
                {
                    team.Members.Add(user);
                }
            }

            await teamRepository.SaveAsync(token);
            await trans.CommitAsync(token);

            logger.Log(Program.StaticLocalizer[nameof(Resources.Program.Admin_UserBatchAdded), users.Count],
                currentUser, TaskStatus.Success);

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
    {
        var loweredHint = hint.ToLower();
        UserInfo[] data = await userManager.Users.Where(item =>
            item.UserName!.ToLower().Contains(loweredHint) ||
            item.StdNumber.ToLower().Contains(loweredHint) ||
            item.Email!.ToLower().Contains(loweredHint) ||
            item.PhoneNumber!.ToLower().Contains(loweredHint) ||
            item.Id.ToString().ToLower().Contains(loweredHint) ||
            item.RealName.ToLower().Contains(loweredHint)
        ).OrderBy(e => e.Id).Take(30).ToArrayAsync(token);

        return Ok(data.Select(UserInfoModel.FromUserInfo).ToResponse());
    }

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
    public async Task<IActionResult> Teams([FromQuery] int count = 100, [FromQuery] int skip = 0,
        CancellationToken token = default) =>
        Ok((await teamRepository.GetTeams(count, skip, token))
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
    public async Task<IActionResult> SearchTeams([FromQuery] string hint, CancellationToken token = default) =>
        Ok((await teamRepository.SearchTeams(hint, token))
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
    [HttpPut("Teams/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTeam([FromRoute] int id, [FromBody] AdminTeamModel model,
        CancellationToken token = default)
    {
        Team? team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Team_NotFound)]));

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
        UserInfo? user = await userManager.FindByIdAsync(userid);

        if (user is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Admin_UserNotFound)],
                StatusCodes.Status404NotFound));

        if (model.UserName is not null && model.UserName != user.UserName)
        {
            IdentityResult result = await userManager.SetUserNameAsync(user, model.UserName);

            if (!result.Succeeded)
                return HandleIdentityError(result.Errors);
        }

        if (model.Email is not null && model.Email != user.Email)
        {
            IdentityResult result = await userManager.SetEmailAsync(user, model.Email);

            if (!result.Succeeded)
                return HandleIdentityError(result.Errors);
        }

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
    [HttpDelete("Users/{userid:guid}/Password")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(string userid)
    {
        UserInfo? user = await userManager.FindByIdAsync(userid);

        if (user is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Admin_UserNotFound)],
                StatusCodes.Status404NotFound));

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
    [HttpDelete("Users/{userid:guid}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid userid, CancellationToken token = default)
    {
        UserInfo? user = await userManager.GetUserAsync(User);

        if (user!.Id == userid)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Admin_SelfDeletionNotAllowed)]));

        user = await userManager.FindByIdAsync(userid.ToString());

        if (user is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Admin_UserNotFound)],
                StatusCodes.Status404NotFound));

        if (await teamRepository.CheckIsCaptain(user, token))
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Admin_CaptainDeletionNotAllowed)]));

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
    [HttpDelete("Teams/{id:int}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTeam(int id, CancellationToken token = default)
    {
        Team? team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Team_NotFound)],
                StatusCodes.Status404NotFound));

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
    [HttpGet("Users/{userid:guid}")]
    [ProducesResponseType(typeof(ProfileUserInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UserInfo(string userid)
    {
        UserInfo? user = await userManager.FindByIdAsync(userid);

        if (user is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Admin_UserNotFound)],
                StatusCodes.Status404NotFound));

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
    public async Task<IActionResult> Logs([FromQuery] string? level = "All", [FromQuery] int count = 50,
        [FromQuery] int skip = 0, CancellationToken token = default) =>
        Ok(await logRepository.GetLogs(skip, count, level, token));

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
    [HttpPut("Participation/{id:int}/{status}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Participation(int id, ParticipationStatus status,
        CancellationToken token = default)
    {
        Participation? participation = await participationRepository.GetParticipationById(id, token);

        if (participation is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Admin_ParticipationNotFound)],
                StatusCodes.Status404NotFound));

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
    [HttpGet("Writeups/{id:int}")]
    [ProducesResponseType(typeof(WriteupInfoModel[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Writeups(int id, CancellationToken token = default)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

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
    [HttpGet("Writeups/{id:int}/All")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAllWriteups(int id, CancellationToken token = default)
    {
        Game? game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        WriteupInfoModel[] wps = await participationRepository.GetWriteups(game, token);
        var filename = $"Writeups-{game.Title}-{DateTimeOffset.UtcNow:yyyyMMdd-HH.mm.ssZ}";
        Stream stream = await Codec.ZipFilesAsync(wps.Select(p => p.File), FilePath.Uploads, filename, token);
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
    public async Task<IActionResult> Instances(CancellationToken token = default) =>
        Ok(new ArrayResponse<ContainerInstanceModel>(await containerRepository.GetContainerInstances(token)));

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
    [SuppressMessage("ReSharper", "RouteTemplates.ParameterTypeCanBeMadeStricter")]
    public async Task<IActionResult> DestroyInstance(Guid id, CancellationToken token = default)
    {
        Container? container = await containerRepository.GetContainerById(id, token);

        if (container is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Admin_ContainerInstanceNotFound)],
                StatusCodes.Status404NotFound));

        if (await containerRepository.DestroyContainer(container, token))
            return Ok();

        return BadRequest(
            new RequestResponse(localizer[nameof(Resources.Program.Admin_ContainerInstanceDestroyFailed)]));
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
    public async Task<IActionResult> Files([FromQuery] int count = 50, [FromQuery] int skip = 0,
        CancellationToken token = default) =>
        Ok(new ArrayResponse<LocalFile>(await fileService.GetFiles(count, skip, token)));

    IActionResult HandleIdentityError(IEnumerable<IdentityError> errors) =>
        BadRequest(new RequestResponse(errors.FirstOrDefault()?.Description ??
                                       localizer[nameof(Resources.Program.Identity_UnknownError)]));
}
