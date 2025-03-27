using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using FluentStorage.Blobs;
using GZCTF.Extensions;
using GZCTF.Middlewares;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Admin;
using GZCTF.Models.Request.Info;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using GZCTF.Services.Config;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace GZCTF.Controllers;

/// <summary>
/// Administration APIs
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
    IBlobStorage storage,
    IBlobRepository blobService,
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
    /// Get configuration
    /// </summary>
    /// <remarks>
    /// Use this API to get global settings, requires Admin permission
    /// </remarks>
    /// <response code="200">Global configuration</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
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
    /// Change configuration
    /// </summary>
    /// <remarks>
    /// Use this API to change global settings, requires Admin permission
    /// </remarks>
    /// <response code="200">Update successful</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    [HttpPut("Config")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateConfigs([FromBody] ConfigEditModel model, CancellationToken token)
    {
        foreach (var prop in typeof(ConfigEditModel).GetProperties())
        {
            var value = prop.GetValue(model);

            if (value is null)
                continue;

            await configService.SaveConfig(prop.PropertyType, value, token);
        }

        return Ok();
    }

    /// <summary>
    /// Change platform Logo
    /// </summary>
    /// <remarks>
    /// Use this API to change the platform Logo, requires Admin permission
    /// </remarks>
    /// <response code="200">Update successful</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    [HttpPost("Config/Logo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateLogo(IFormFile file, CancellationToken token)
    {
        switch (file.Length)
        {
            case 0:
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_SizeZero)]));
            case > 3 * 1024 * 1024:
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_SizeTooLarge)]));
        }

        if (!await DeleteCurrentLogo(token))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Admin_LogoUpdateFailed)]));

        var logo = await blobService.CreateOrUpdateImage(file, "logo", 640, token);
        if (logo is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Admin_LogoUpdateFailed)]));

        var favicon = await blobService.CreateOrUpdateImage(file, "favicon", 256, token);
        if (favicon is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Admin_LogoUpdateFailed)]));

        HashSet<Config> configSet =
        [
            new($"{nameof(GlobalConfig)}:{nameof(GlobalConfig.LogoHash)}", logo.Hash, [CacheKey.ClientConfig]),
            new($"{nameof(GlobalConfig)}:{nameof(GlobalConfig.FaviconHash)}", favicon.Hash, [CacheKey.Favicon])
        ];

        await configService.SaveConfigSet(configSet, token);

        return Ok();
    }

    /// <summary>
    /// Reset platform Logo
    /// </summary>
    /// <remarks>
    /// Use this API to reset the platform Logo, requires Admin permission
    /// </remarks>
    /// <response code="200">Updated successfully</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    [HttpDelete("Config/Logo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetLogo(CancellationToken token)
    {
        if (!await DeleteCurrentLogo(token))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Admin_LogoUpdateFailed)]));

        HashSet<Config> configSet =
        [
            new($"{nameof(GlobalConfig)}:{nameof(GlobalConfig.LogoHash)}", string.Empty, [CacheKey.ClientConfig]),
            new($"{nameof(GlobalConfig)}:{nameof(GlobalConfig.FaviconHash)}", string.Empty, [CacheKey.Favicon])
        ];

        await configService.SaveConfigSet(configSet, token);

        return Ok();
    }

    async Task<bool> DeleteCurrentLogo(CancellationToken token)
    {
        var globalConfig = serviceProvider.GetRequiredService<IOptionsSnapshot<GlobalConfig>>().Value;

        return await DeleteByHash(globalConfig.LogoHash, token) &&
               await DeleteByHash(globalConfig.FaviconHash, token);
    }

    async Task<bool> DeleteByHash(string? hash, CancellationToken token)
    {
        if (hash is not null && Codec.FileHashRegex().IsMatch(hash))
            return await blobService.DeleteBlobByHash(hash, token) switch
            {
                TaskStatus.Success or TaskStatus.NotFound => true,
                _ => false
            };

        return true;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    /// <remarks>
    /// Use this API to get all users, requires Admin permission
    /// </remarks>
    /// <response code="200">User list</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    [HttpGet("Users")]
    [ProducesResponseType(typeof(ArrayResponse<UserInfoModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Users([FromQuery][Range(0, 500)] int count = 100, [FromQuery] int skip = 0,
        CancellationToken token = default) =>
        Ok((await userManager.Users.OrderBy(e => e.Id).Skip(skip).Take(count)
                .Select(u => UserInfoModel.FromUserInfo(u))
                .ToArrayAsync(token))
            .ToResponse(await userManager.Users.CountAsync(token)));

    /// <summary>
    /// Add users in batch
    /// </summary>
    /// <remarks>
    /// Use this API to add users in batch, requires Admin permission
    /// </remarks>
    /// <response code="200">Successfully added</response>
    /// <response code="400">User validation failed</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
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

                if (result.Succeeded)
                {
                    users.Add((userInfo, user.TeamName));
                    continue;
                }

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
            foreach ((var user, var teamName) in users)
            {
                if (teamName is null)
                    continue;

                var team = teams.Find(team => team.Name == teamName);
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

            logger.Log(StaticLocalizer[nameof(Resources.Program.Admin_UserBatchAdded), users.Count],
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
    /// Search users
    /// </summary>
    /// <remarks>
    /// Use this API to search users, requires Admin permission
    /// </remarks>
    /// <response code="200">User list</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    [HttpPost("Users/Search")]
    [ProducesResponseType(typeof(ArrayResponse<UserInfoModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchUsers([FromQuery] string hint, CancellationToken token = default)
    {
        var loweredHint = hint.ToLower();
        var data = await userManager.Users.Where(item =>
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
    /// Get all team information
    /// </summary>
    /// <remarks>
    /// Use this API to get all teams, requires Admin permission
    /// </remarks>
    /// <response code="200">User list</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    [HttpGet("Teams")]
    [ProducesResponseType(typeof(ArrayResponse<TeamInfoModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Teams([FromQuery][Range(0, 500)] int count = 100, [FromQuery] int skip = 0,
        CancellationToken token = default) =>
        Ok((await teamRepository.GetTeams(count, skip, token)).Select(team => TeamInfoModel.FromTeam(team))
            .ToResponse(await teamRepository.CountAsync(token)));

    /// <summary>
    /// Search teams
    /// </summary>
    /// <remarks>
    /// Use this API to search teams, requires Admin permission
    /// </remarks>
    /// <response code="200">User list</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    [HttpPost("Teams/Search")]
    [ProducesResponseType(typeof(ArrayResponse<TeamInfoModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchTeams([FromQuery] string hint, CancellationToken token = default) =>
        Ok((await teamRepository.SearchTeams(hint, token))
            .Select(team => TeamInfoModel.FromTeam(team))
            .ToResponse());

    /// <summary>
    /// Modify team information
    /// </summary>
    /// <remarks>
    /// Use this API to modify team information, requires Admin permission
    /// </remarks>
    /// <response code="200">Successfully updated</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Team not found</response>
    [HttpPut("Teams/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTeam([FromRoute] int id, [FromBody] AdminTeamModel model,
        CancellationToken token = default)
    {
        var team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Team_NotFound)]));

        team.UpdateInfo(model);
        await teamRepository.SaveAsync(token);

        return Ok();
    }

    /// <summary>
    /// Modify user information
    /// </summary>
    /// <remarks>
    /// Use this API to modify user information, requires Admin permission
    /// </remarks>
    /// <response code="200">Successfully updated</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">User not found</response>
    [HttpPut("Users/{userid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserInfo(string userid, [FromBody] AdminUserInfoModel model)
    {
        var user = await userManager.FindByIdAsync(userid);

        if (user is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Admin_UserNotFound)],
                StatusCodes.Status404NotFound));

        if (model.UserName is not null && model.UserName != user.UserName)
        {
            var result = await userManager.SetUserNameAsync(user, model.UserName);

            if (!result.Succeeded)
                return HandleIdentityError(result.Errors);
        }

        if (model.Email is not null && model.Email != user.Email)
        {
            var result = await userManager.SetEmailAsync(user, model.Email);

            if (!result.Succeeded)
                return HandleIdentityError(result.Errors);
        }

        user.UpdateUserInfo(model);
        await userManager.UpdateAsync(user);

        return Ok();
    }

    /// <summary>
    /// Reset user password
    /// </summary>
    /// <remarks>
    /// Use this API to reset user password, requires Admin permission
    /// </remarks>
    /// <response code="200">Successfully retrieved</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">User not found</response>
    [HttpDelete("Users/{userid:guid}/Password")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(string userid)
    {
        var user = await userManager.FindByIdAsync(userid);

        if (user is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Admin_UserNotFound)],
                StatusCodes.Status404NotFound));

        var pwd = Codec.RandomPassword(16);
        var code = await userManager.GeneratePasswordResetTokenAsync(user);
        await userManager.ResetPasswordAsync(user, code, pwd);

        return Ok(pwd);
    }

    /// <summary>
    /// Delete user
    /// </summary>
    /// <remarks>
    /// Use this API to delete user, requires Admin permission
    /// </remarks>
    /// <response code="200">Successfully retrieved</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">User not found</response>
    [HttpDelete("Users/{userid:guid}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid userid, CancellationToken token = default)
    {
        var user = await userManager.GetUserAsync(User);

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
    /// Delete team
    /// </summary>
    /// <remarks>
    /// Use this API to delete team, requires Admin permission
    /// </remarks>
    /// <response code="200">Successfully retrieved</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">User not found</response>
    [HttpDelete("Teams/{id:int}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTeam(int id, CancellationToken token = default)
    {
        var team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Team_NotFound)],
                StatusCodes.Status404NotFound));

        await teamRepository.DeleteTeam(team, token);

        return Ok();
    }

    /// <summary>
    /// Get user information
    /// </summary>
    /// <remarks>
    /// Use this API to get user information, requires Admin permission
    /// </remarks>
    /// <response code="200">User object</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    [HttpGet("Users/{userid:guid}")]
    [ProducesResponseType(typeof(ProfileUserInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UserInfo(string userid)
    {
        var user = await userManager.FindByIdAsync(userid);

        if (user is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Admin_UserNotFound)],
                StatusCodes.Status404NotFound));

        return Ok(ProfileUserInfoModel.FromUserInfo(user));
    }

    /// <summary>
    /// Get all logs
    /// </summary>
    /// <remarks>
    /// Use this API to get all logs, requires Admin permission
    /// </remarks>
    /// <response code="200">Log list</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    [HttpGet("Logs")]
    [ProducesResponseType(typeof(LogMessageModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logs([FromQuery] string? level = "All",
        [FromQuery][Range(0, 1000)] int count = 50,
        [FromQuery] int skip = 0, CancellationToken token = default) =>
        Ok(await logRepository.GetLogs(skip, count, level, token));

    /// <summary>
    /// Update participation status
    /// </summary>
    /// <remarks>
    /// Use this API to update team participation status, review application, requires Admin permission
    /// </remarks>
    /// <response code="200">Update successful</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Participation object not found</response>
    [HttpPut("Participation/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Participation(int id, [FromBody] ParticipationEditModel model,
        CancellationToken token = default)
    {
        var participation = await participationRepository.GetParticipationById(id, token);

        if (participation is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Admin_ParticipationNotFound)],
                StatusCodes.Status404NotFound));

        await participationRepository.UpdateParticipation(participation, model, token);

        return Ok();
    }

    /// <summary>
    /// Get all Writeup basic information
    /// </summary>
    /// <remarks>
    /// Use this API to get Writeup basic information, requires Admin permission
    /// </remarks>
    /// <response code="200">Update successful</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Game not found</response>
    [HttpGet("Writeups/{id:int}")]
    [ProducesResponseType(typeof(WriteupInfoModel[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Writeups(int id, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        return Ok(await participationRepository.GetWriteups(game, token));
    }

    /// <summary>
    /// Download all Writeups
    /// </summary>
    /// <remarks>
    /// Use this API to download all Writeups, requires Admin permission
    /// </remarks>
    /// <response code="200">Downloaded successfully</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Game not found</response>
    [HttpGet("Writeups/{id:int}/All")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAllWriteups(int id, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var wps = await participationRepository.GetWriteups(game, token);
        var filename = $"Writeups-{game.Title}-{DateTimeOffset.UtcNow:yyyyMMdd-HH.mm.ssZ}";

        return new TarFilesResult(storage, wps.Select(p => p.File), PathHelper.Uploads, filename, token);
    }

    /// <summary>
    /// Get all container instances
    /// </summary>
    /// <remarks>
    /// Use this API to get all container instances, requires Admin permission
    /// </remarks>
    /// <response code="200">Instance list</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    [HttpGet("Instances")]
    [ProducesResponseType(typeof(ArrayResponse<ContainerInstanceModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Instances(CancellationToken token = default) =>
        Ok(new ArrayResponse<ContainerInstanceModel>(await containerRepository.GetContainerInstances(token)));

    /// <summary>
    /// Delete container instance
    /// </summary>
    /// <remarks>
    /// Use this API to forcibly delete container instance, requires Admin permission
    /// </remarks>
    /// <response code="200">Successfully retrieved</response>
    /// <response code="400">Container instance destruction failed</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Container instance not found</response>
    [HttpDelete("Instances/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [SuppressMessage("ReSharper", "RouteTemplates.ParameterTypeCanBeMadeStricter")]
    public async Task<IActionResult> DestroyInstance(Guid id, CancellationToken token = default)
    {
        var container = await containerRepository.GetContainerById(id, token);

        if (container is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Admin_ContainerInstanceNotFound)],
                StatusCodes.Status404NotFound));

        if (await containerRepository.DestroyContainer(container, token))
            return Ok();

        return BadRequest(
            new RequestResponse(localizer[nameof(Resources.Program.Admin_ContainerInstanceDestroyFailed)]));
    }

    /// <summary>
    /// Get all files
    /// </summary>
    /// <remarks>
    /// Use this API to get all files, requires Admin permission
    /// </remarks>
    /// <response code="200">File list</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    [HttpGet("Files")]
    [ProducesResponseType(typeof(ArrayResponse<LocalFile>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Files([FromQuery][Range(0, 500)] int count = 50, [FromQuery] int skip = 0,
        CancellationToken token = default) =>
        Ok(new ArrayResponse<LocalFile>(await blobService.GetBlobs(count, skip, token)));

    IActionResult HandleIdentityError(IEnumerable<IdentityError> errors) =>
        BadRequest(new RequestResponse(errors.FirstOrDefault()?.Description ??
                                       localizer[nameof(Resources.Program.Identity_UnknownError)]));
}
