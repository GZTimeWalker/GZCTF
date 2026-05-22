using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using GZCTF.Extensions;
using GZCTF.Middlewares;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Admin;
using GZCTF.Models.Request.Info;
using GZCTF.Models.Request.Game;
using GZCTF.Models.Response.Admin;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using GZCTF.Services.Config;
using GZCTF.Services.Mail;
using GZCTF.Storage.Interface;
using GZCTF.Utils;
using GZCTF.Models.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace GZCTF.Controllers;

/// <summary>
/// Administration APIs
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class AdminController(
    UserManager<UserInfo> userManager,
    ILogger<AdminController> logger,
    IBlobStorage storage,
    CacheHelper cacheHelper,
    IBlobRepository blobService,
    ILogRepository logRepository,
    IConfigService configService,
    IGameRepository gameRepository,
    ITeamRepository teamRepository,
    IContainerRepository containerRepository,
    IServiceProvider serviceProvider,
    IParticipationRepository participationRepository,

    IChallengeReviewRepository challengeReviewRepository,
    ICheatInfoRepository cheatInfoRepository,
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
    [RequireAdmin]
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
    [RequireAdmin]
    [HttpPut("Config")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateConfigs([FromBody] ConfigEditModel model, CancellationToken token)
    {
        // handle api encryption config
        var global = serviceProvider.GetRequiredService<IOptionsSnapshot<GlobalConfig>>().Value;
        if (!global.ApiEncryption && model.GlobalConfig?.ApiEncryption is true)
            await configService.UpdateApiEncryptionKey(token);

        // save all config properties
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
    [RequireAdmin]
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
    [RequireAdmin]
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

    private async Task<bool> DeleteCurrentLogo(CancellationToken token)
    {
        var globalConfig = serviceProvider.GetRequiredService<IOptionsSnapshot<GlobalConfig>>().Value;

        return await DeleteByHash(globalConfig.LogoHash, token) &&
               await DeleteByHash(globalConfig.FaviconHash, token);
    }

    private async Task<bool> DeleteByHash(string? hash, CancellationToken token)
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
    [RequireAdmin]
    [HttpGet("Users")]
    [ProducesResponseType(typeof(ArrayResponse<UserInfoModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Users([FromQuery][Range(0, 500)] int count = 100, [FromQuery] int skip = 0,
        [FromQuery] string? search = null, CancellationToken token = default)
    {
        var query = userManager.Users.AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(u => u.UserName!.Contains(search) || u.Email!.Contains(search));

        return Ok((await query.OrderBy(e => e.Id).Skip(skip).Take(count)
                .Select(u => UserInfoModel.FromUserInfo(u))
                .ToArrayAsync(token))
            .ToResponse(await query.CountAsync(token)));
    }

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
    [RequireAdmin]
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
            foreach (var (user, teamName) in users)
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
    /// Import users from pre-parsed, user-reviewed rows
    /// </summary>
    /// <remarks>
    /// Accepts structured rows (after client-side CSV parsing and user editing).
    /// Auto-generates unique usernames and secure passwords server-side, creates accounts
    /// and teams in a single atomic transaction, and returns the full credentials list.
    /// Rate-limit-safe: one HTTP call regardless of import size.
    /// </remarks>
    /// <response code="200">Import complete — returns per-user credentials and summary counts</response>
    /// <response code="400">No rows provided or request is invalid</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [RequireAdmin]
    [HttpPost("Users/Import")]
    [ProducesResponseType(typeof(CsvImportResultModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportUsersFromCsv([FromBody] CsvImportRequest request, CancellationToken token = default)
    {
        if (request.Rows is null || request.Rows.Count == 0)
            return BadRequest(new RequestResponse("No rows provided"));

        string? NE(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        var takenUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var toCreate = new List<(UserCreateModel Model, string RealName)>(request.Rows.Count);
        var skipped = new List<CsvImportUserResult>();

        foreach (var row in request.Rows)
        {
            var realName = row.RealName?.Trim() ?? string.Empty;
            var email = row.Email?.Trim().ToLowerInvariant() ?? string.Empty;

            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            {
                skipped.Add(new CsvImportUserResult { Email = email, RealName = realName, Status = "skipped", Error = "Invalid or missing email" });
                continue;
            }

            if (!seenEmails.Add(email))
            {
                skipped.Add(new CsvImportUserResult { Email = email, RealName = realName, Status = "skipped", Error = "Duplicate email" });
                continue;
            }

            // Username: use explicit override if provided, otherwise auto-generate from real name
            var username = !string.IsNullOrWhiteSpace(row.UserNameOverride)
                ? CsvEnsureUniqueUsername(row.UserNameOverride.Trim(), takenUsernames)
                : CsvGenerateUsername(realName, takenUsernames);

            string? teamName = request.TeamMode switch
            {
                "single" => NE(request.SingleTeamName),
                "fromrow" => NE(row.TeamName),
                _ => null
            };

            toCreate.Add((new UserCreateModel
            {
                UserName = username,
                Password = CsvGeneratePassword(),
                Email = email,
                RealName = NE(realName),
                StdNumber = NE(row.StdNumber),
                Phone = NE(row.Phone),
                TeamName = teamName,
            }, realName));
        }

        var currentUser = await userManager.GetUserAsync(User);
        var trans = await teamRepository.BeginTransactionAsync(token);
        var results = new List<CsvImportUserResult>(toCreate.Count);

        try
        {
            var created = new List<(UserInfo User, string? TeamName)>(toCreate.Count);

            foreach (var (model, realName) in toCreate)
            {
                var userInfo = model.ToUserInfo();
                userInfo.EmailConfirmed = request.EmailConfirmed;

                var result = await userManager.CreateAsync(userInfo, model.Password);
                string status = "created";

                if (!result.Succeeded)
                {
                    var errorCode = result.Errors.FirstOrDefault()?.Code;
                    userInfo = errorCode switch
                    {
                        "DuplicateEmail" => await userManager.FindByEmailAsync(model.Email),
                        "DuplicateUserName" => await userManager.FindByNameAsync(model.UserName),
                        _ => null
                    };

                    if (userInfo is null)
                    {
                        results.Add(new CsvImportUserResult
                        {
                            Email = model.Email, RealName = realName, UserName = model.UserName,
                            Status = "skipped", Error = result.Errors.FirstOrDefault()?.Description
                        });
                        continue;
                    }

                    userInfo.UpdateUserInfo(model);
                    var resetCode = await userManager.GeneratePasswordResetTokenAsync(userInfo);
                    await userManager.ResetPasswordAsync(userInfo, resetCode, model.Password);
                    status = "updated";
                }

                created.Add((userInfo, model.TeamName));
                results.Add(new CsvImportUserResult
                {
                    Email = model.Email,
                    RealName = realName,
                    UserName = userInfo.UserName ?? model.UserName,
                    Password = model.Password,
                    TeamName = model.TeamName,
                    Status = status
                });
            }

            var teams = new List<Team>();
            foreach (var (user, teamName) in created)
            {
                if (teamName is null) continue;
                var team = teams.Find(t => t.Name == teamName);
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

            logger.Log(StaticLocalizer[nameof(Resources.Program.Admin_UserBatchAdded), results.Count(r => r.Status == "created")],
                currentUser, TaskStatus.Success);
        }
        catch
        {
            await trans.RollbackAsync(token);
            throw;
        }

        results.AddRange(skipped);

        return Ok(new CsvImportResultModel
        {
            Total = request.Rows.Count,
            Created = results.Count(r => r.Status == "created"),
            Updated = results.Count(r => r.Status == "updated"),
            Skipped = results.Count(r => r.Status == "skipped"),
            Users = results
        });
    }

    /// <summary>
    /// Batch-send credential emails to imported users
    /// </summary>
    /// <remarks>
    /// Sends one email per item using a single SMTP connection.
    /// Passwords are provided by the caller (from the import result) and are not stored.
    /// Returns sent/failed counts.
    /// </remarks>
    /// <response code="200">Email send complete — returns sent and failed counts</response>
    /// <response code="400">No items provided</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [RequireAdmin]
    [HttpPost("Users/Credentials/Send")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendCredentialEmails([FromBody] SendCredentialsRequest request,
        CancellationToken token = default)
    {
        if (request.Items is null || request.Items.Count == 0)
            return BadRequest(new RequestResponse("No credentials provided"));

        var mailSender = serviceProvider.GetRequiredService<IMailSender>();
        var globalConfig = serviceProvider.GetRequiredService<IOptionsSnapshot<GlobalConfig>>();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        // Build (UserName, Email, ResetLink) tuples — one password-reset token per user
        var resetItems = new List<(string UserName, string Email, string ResetLink)>(request.Items.Count);
        foreach (var item in request.Items)
        {
            var user = await userManager.FindByEmailAsync(item.Email);
            if (user is null) continue;

            var rawToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = Codec.Base64.Encode(rawToken);
            var encodedEmail = Codec.Base64.Encode(item.Email);
            var resetLink = $"{baseUrl}/account/reset?token={encodedToken}&email={encodedEmail}";
            resetItems.Add((item.UserName, item.Email, resetLink));
        }

        var (sent, failed) = await mailSender.SendCredentialsBatch(resetItems, baseUrl, localizer, globalConfig, token);
        failed += request.Items.Count - resetItems.Count; // users not found count as failed

        logger.Log(
            StaticLocalizer[nameof(Resources.Program.Admin_UserBatchAdded), sent],
            await userManager.GetUserAsync(User), TaskStatus.Success);

        return Ok(new { sent, failed });
    }

    // ─── CSV import helpers ───────────────────────────────────────────────────

    private static string CsvEnsureUniqueUsername(string desired, HashSet<string> taken, int maxLen = 15)
    {
        var @base = desired.Length > maxLen ? desired[..maxLen] : desired;
        var name = @base;
        for (int i = 1; taken.Contains(name); i++)
        {
            var suf = i.ToString();
            name = (@base.Length + suf.Length > maxLen ? @base[..(maxLen - suf.Length)] : @base) + suf;
        }
        taken.Add(name);
        return name;
    }

    private static string CsvGenerateUsername(string realName, HashSet<string> taken, int maxLen = 15)
    {
        var clean = Regex.Replace(realName.ToLowerInvariant().Replace(" ", "."), @"[^a-z0-9.]", string.Empty);
        var @base = clean.Length > 0 ? (clean.Length > maxLen ? clean[..maxLen] : clean) : "user";
        var name = @base;
        for (int i = 1; taken.Contains(name); i++)
        {
            var suf = i.ToString();
            name = (@base.Length + suf.Length > maxLen ? @base[..(maxLen - suf.Length)] : @base) + suf;
        }
        taken.Add(name);
        return name;
    }

    private static string CsvGeneratePassword()
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";
        const string all = upper + lower + digits + special;

        var rng = RandomNumberGenerator.GetBytes(32);
        var chars = new char[16];
        // Guarantee at least one character from each required class
        chars[0] = upper[rng[0] % upper.Length];
        chars[1] = lower[rng[1] % lower.Length];
        chars[2] = digits[rng[2] % digits.Length];
        chars[3] = special[rng[3] % special.Length];
        for (int i = 4; i < 16; i++)
            chars[i] = all[rng[i] % all.Length];
        // Fisher-Yates shuffle
        for (int i = 15; i > 0; i--)
        {
            int j = rng[16 + i] % (i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }

    private static List<string[]> ParseCsvContent(string content)
    {
        var result = new List<string[]>();
        using var reader = new StringReader(content);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var fields = new List<string>();
            var field = new StringBuilder();
            bool inQuote = false;
            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '"')
                {
                    if (inQuote && i + 1 < line.Length && line[i + 1] == '"') { field.Append('"'); i++; }
                    else inQuote = !inQuote;
                }
                else if (c == ',' && !inQuote) { fields.Add(field.ToString().Trim()); field.Clear(); }
                else field.Append(c);
            }
            fields.Add(field.ToString().Trim());
            result.Add([.. fields]);
        }
        return result;
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
    [RequireAdmin]
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
    [RequireAdmin]
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
    [RequireAdmin]
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
    [RequireAdmin]
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
    [RequireAdmin]
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
    [RequireAdmin]
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
    [RequireAdmin]
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
    [RequireAdmin]
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
    [RequireAdmin]
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
    /// <param name="level"></param>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="search">Search query</param>
    /// <param name="token"></param>
    /// <response code="200">Log list</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    [RequireAdmin]
    [HttpGet("Logs")]
    [ProducesResponseType(typeof(LogMessageModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logs([FromQuery] string? level = "All",
        [FromQuery][Range(0, 1000)] int count = 50,
        [FromQuery] int skip = 0, [FromQuery] string? search = null, CancellationToken token = default) =>
        Ok(await logRepository.GetLogs(skip, count, level, search, token));

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
    [RequireUser]
    [HttpPut("Participation/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Participation(int id, [FromBody] ParticipationEditModel model,
        CancellationToken token = default)
    {
        await using var transaction = await participationRepository.BeginTransactionAsync(token);

        var participation = await participationRepository.GetParticipationById(id, token);

        if (participation is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Admin_ParticipationNotFound)],
                StatusCodes.Status404NotFound));

        var currentUser = await userManager.GetUserAsync(User);
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
        if (currentUser?.Role != Role.Admin &&
            !await dbContext.EventManagers.AnyAsync(em => em.UserId == currentUser!.Id && em.GameId == participation.GameId, token))
            return Forbid();

        await participationRepository.UpdateParticipation(participation, model, token);

        await transaction.CommitAsync(token);
        await cacheHelper.FlushScoreboardCache(participation.GameId, token);

        return Ok();
    }

    /// <summary>
    /// Get per-challenge health stats for a game
    /// </summary>
    /// <remarks>
    /// Returns solve count, wrong attempt count, wrong rate, first-solve time, and solving-team count
    /// for every enabled challenge in the game. Requires GameAdmin permission.
    /// </remarks>
    /// <response code="200">Challenge health stats</response>
    /// <response code="404">Game not found</response>
    [RequireGameAdmin]
    [HttpGet("Games/{id:int}/Challenges/Health")]
    [ProducesResponseType(typeof(ChallengeHealthModel[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChallengeHealth([FromRoute] int id, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);
        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

        var challenges = await dbContext.GameChallenges
            .AsNoTracking()
            .Where(c => c.GameId == id && c.IsEnabled)
            .Select(c => new { c.Id, c.Title, Category = c.Category.ToString() })
            .ToListAsync(token);

        var solveStats = await dbContext.Submissions
            .AsNoTracking()
            .Where(s => s.GameId == id && s.Status == AnswerResult.Accepted)
            .GroupBy(s => s.ChallengeId)
            .Select(g => new
            {
                ChallengeId = g.Key,
                Count = g.Count(),
                TeamCount = g.Select(s => s.TeamId).Distinct().Count(),
                FirstTime = g.Min(s => (DateTimeOffset?)s.SubmitTimeUtc)
            })
            .ToListAsync(token);

        var wrongStats = await dbContext.Submissions
            .AsNoTracking()
            .Where(s => s.GameId == id && s.Status == AnswerResult.WrongAnswer)
            .GroupBy(s => s.ChallengeId)
            .Select(g => new { ChallengeId = g.Key, Count = g.Count() })
            .ToListAsync(token);

        var solveMap = solveStats.ToDictionary(x => x.ChallengeId);
        var wrongMap = wrongStats.ToDictionary(x => x.ChallengeId);

        var result = challenges.Select(c => new ChallengeHealthModel
        {
            Id = c.Id,
            Title = c.Title,
            Category = c.Category,
            SolveCount = solveMap.TryGetValue(c.Id, out var s) ? s.Count : 0,
            SolveTeamCount = solveMap.TryGetValue(c.Id, out var st) ? st.TeamCount : 0,
            WrongCount = wrongMap.TryGetValue(c.Id, out var w) ? w.Count : 0,
            FirstSolveTime = solveMap.TryGetValue(c.Id, out var fs) ? fs.FirstTime : null,
        }).ToArray();

        return Ok(result);
    }

    /// <summary>
    /// Get flag-egress events for a game
    /// </summary>
    /// <remarks>
    /// Returns flag-egress events (admin live feed). Each row represents a sliding-window
    /// aggregation of hits where the team's per-team dynamic flag was observed in
    /// proxied container traffic. Requires GameAdmin permission.
    /// </remarks>
    /// <response code="200">Flag egress events page</response>
    /// <response code="404">Game not found</response>
    [RequireGameAdmin]
    [HttpGet("Games/{id:int}/FlagEgress")]
    [ProducesResponseType(typeof(ArrayResponse<FlagEgressEventModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFlagEgressEvents(
        [FromRoute] int id,
        [FromQuery] int skip = 0,
        [FromQuery] int count = 50,
        CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);
        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        count = Math.Clamp(count, 1, 200);
        skip = Math.Max(0, skip);

        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

        var total = await dbContext.FlagEgressEvents.AsNoTracking()
            .Where(e => e.GameId == id)
            .CountAsync(token);

        var rows = await dbContext.FlagEgressEvents.AsNoTracking()
            .Where(e => e.GameId == id)
            .OrderByDescending(e => e.LastSeenUtc)
            .Skip(skip)
            .Take(count)
            .Select(e => new FlagEgressEventModel
            {
                Id = e.Id,
                GameId = e.GameId,
                ParticipationId = e.ParticipationId,
                ChallengeId = e.ChallengeId,
                ContainerId = e.ContainerId,
                TeamName = e.Participation.Team.Name,
                ChallengeTitle = e.Challenge.Title,
                RemoteIp = e.RemoteIp,
                RemotePort = e.RemotePort,
                HitCount = e.HitCount,
                FirstSeenUtc = e.FirstSeenUtc,
                LastSeenUtc = e.LastSeenUtc,
                Direction = e.Direction,
            })
            .ToArrayAsync(token);

        return Ok(new ArrayResponse<FlagEgressEventModel>(rows, total));
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
    [RequireGameAdmin]
    [HttpGet("Writeups/{id:int}")]
    [ProducesResponseType(typeof(WriteupInfoModel), StatusCodes.Status200OK)]
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
    [RequireGameAdmin]
    [HttpGet("Writeups/{id:int}/All")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAllWriteups(int id, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var into = await participationRepository.GetWriteups(game, token);
        var filename = $"Writeups-{game.Title}-{DateTimeOffset.UtcNow:yyyyMMdd-HH.mm.ss}Z";

        return new TarFilesResult(storage, into.Writeups.Select(p => p.File), PathHelper.Uploads, filename, token);
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
    [RequireAdmin]
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
    /// <summary>
    /// Sample point-in-time CPU/memory/network stats for a running
    /// container instance. Returns 404 when the instance is gone or the
    /// runtime can't provide stats (e.g. Kubernetes mode in v1).
    /// </summary>
    [RequireAdmin]
    [HttpGet("Instances/{id:guid}/Stats")]
    [ProducesResponseType(typeof(ContainerStatsModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInstanceStats(Guid id,
        [FromServices] Services.Container.Manager.IContainerManager containerManager,
        CancellationToken token = default)
    {
        var container = await containerRepository.GetContainerById(id, token);
        if (container is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Admin_ContainerInstanceNotFound)],
                StatusCodes.Status404NotFound));

        var stats = await containerManager.GetStatsAsync(container, token);
        if (stats is null)
            return NotFound(new RequestResponse("Stats unavailable for this container.",
                StatusCodes.Status404NotFound));

        return Ok(stats);
    }

    [RequireAdmin]
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
    [RequireAdmin]
    [HttpGet("Files")]
    [ProducesResponseType(typeof(ArrayResponse<LocalFile>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Files([FromQuery][Range(0, 500)] int count = 50, [FromQuery] int skip = 0,
        CancellationToken token = default) =>
        Ok(new ArrayResponse<LocalFile>(await blobService.GetBlobs(count, skip, token)));

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    /// <remarks>
    /// Use this API to get dashboard statistics, requires Admin permission
    /// </remarks>
    /// <response code="200">Dashboard statistics</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    [RequireAdmin]
    [HttpGet("Dashboard")]
    [ProducesResponseType(typeof(AdminDashboardModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken token = default)
    {
        var users = await userManager.Users.CountAsync(token);
        var teams = await teamRepository.CountAsync(token);
        var containers = await containerRepository.CountAsync(token);

        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
        
        var topGames = await dbContext.Games
             .AsNoTracking()
             .OrderByDescending(g => g.Participations.Count)
             .Take(5)
             .Select(g => new 
             {
                 g.Id,
                 g.Title,
                 g.StartTimeUtc,
                 g.EndTimeUtc,
                 g.PosterHash,
                 g.TeamMemberCountLimit,
                 TeamCount = g.Teams!.Count,
                 UserCount = g.Participations.Count,
             })
             .ToListAsync(token);

        var gameIds = topGames.Select(x => x.Id).ToList();
        
        var reviewStats = await dbContext.ChallengeReviews
            .Where(r => gameIds.Contains(r.GameId))
            .GroupBy(r => r.GameId)
            .Select(g => new 
            {
                GameId = g.Key,
                ReviewCount = g.Count(),
                AverageRating = g.Where(r => r.Rating == ReviewRating.Like || r.Rating == ReviewRating.Dislike)
                    .Average(r => (double?)(r.Rating == ReviewRating.Like ? 1.0 : 0.0))
            })
            .ToDictionaryAsync(x => x.GameId, token);

        var popularGames = topGames.Select(g => new BasicGameInfoModel
        {
             Id = g.Id,
             Title = g.Title,
             StartTimeUtc = g.StartTimeUtc,
             EndTimeUtc = g.EndTimeUtc,
             PosterHash = g.PosterHash,
             TeamMemberCountLimit = g.TeamMemberCountLimit,
             TeamCount = g.TeamCount,
             UserCount = g.UserCount,
             ReviewCount = reviewStats.GetValueOrDefault(g.Id)?.ReviewCount ?? 0,
             AverageRating = reviewStats.GetValueOrDefault(g.Id)?.AverageRating
        }).ToList();

        return Ok(new AdminDashboardModel
        {
            SystemStats = new()
            {
                UserCount = users,
                TeamCount = teams,
                ActiveContainerCount = containers
            },
            TopGames = popularGames
        });
    }

    /// <summary>
    /// Get all reviews
    /// </summary>
    [RequireAdmin]
    [HttpGet("Reviews")]
    [ProducesResponseType(typeof(IEnumerable<ChallengeReviewDetailModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReviews([FromQuery][Range(1, 1000)] int count = 20, [FromQuery] int skip = 0, CancellationToken token = default)
    {
        var reviews = await challengeReviewRepository.GetAllReviewsAsync(count, skip, token);
        return Ok(reviews.Select(ChallengeReviewDetailModel.FromReview)); 
    }

    /// <summary>
    /// Get submission trend
    /// </summary>
    [RequireAdmin]
    [HttpGet("SubmissionTrend")]
    [ProducesResponseType(typeof(IEnumerable<SubmissionTrendModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubmissionTrend([FromQuery] string range = "Day", CancellationToken token = default)
    {
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        DateTimeOffset since;
        
        switch (range.ToLower())
        {
            case "week":
                since = now.AddDays(-7);
                break;
            case "month":
                since = now.AddDays(-30);
                break;
            case "year":
                since = now.AddYears(-1);
                break;
            case "day":
            default:
                since = now.AddHours(-24);
                break;
        }

        var query = dbContext.Submissions.Where(s => s.SubmitTimeUtc >= since);

        if (range.ToLower() == "year")
        {
            // Group by Month
            return Ok(await query
                .GroupBy(s => new { s.SubmitTimeUtc.Year, s.SubmitTimeUtc.Month })
                .Select(g => new SubmissionTrendModel
                {
                    Time = new DateTime(g.Key.Year, g.Key.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                    Count = g.Count()
                })
                .OrderBy(t => t.Time)
                .ToListAsync(token));
        }
        else if (range.ToLower() == "week" || range.ToLower() == "month")
        {
            // Group by Day
            return Ok(await query
                .GroupBy(s => new { s.SubmitTimeUtc.Year, s.SubmitTimeUtc.Month, s.SubmitTimeUtc.Day })
                .Select(g => new SubmissionTrendModel
                {
                    Time = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, 0, 0, 0, DateTimeKind.Utc),
                    Count = g.Count()
                })
                .OrderBy(t => t.Time)
                .ToListAsync(token));
        }
        else
        {
            // Group by Hour (Default/Day)
            return Ok(await query
                .GroupBy(s => new { s.SubmitTimeUtc.Year, s.SubmitTimeUtc.Month, s.SubmitTimeUtc.Day, s.SubmitTimeUtc.Hour })
                .Select(g => new SubmissionTrendModel
                {
                    Time = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, 0, 0, DateTimeKind.Utc),
                    Count = g.Count()
                })
                .OrderBy(t => t.Time)
                .ToListAsync(token));
        }
    }

    /// <summary>
    /// Get all cheat reports
    /// </summary>
    [RequireAdmin]
    [HttpGet("CheatReports")]
    [ProducesResponseType(typeof(IEnumerable<CheatInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCheatReports([FromQuery][Range(1, 1000)] int count = 20, [FromQuery] int skip = 0, CancellationToken token = default)
    {
        var cheats = await cheatInfoRepository.GetAllCheatInfosAsync(count, skip, token);
        return Ok(cheats);
    }

    /// <summary>
    /// Get all writeups
    /// </summary>
    [RequireAdmin]
    [HttpGet("AllWriteups")]
    [ProducesResponseType(typeof(IEnumerable<WriteupInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllWriteups([FromQuery][Range(1, 1000)] int count = 20, [FromQuery] int skip = 0, CancellationToken token = default)
    {
        var writeups = await participationRepository.GetAllWriteupsAsync(count, skip, token);
        return Ok(writeups);
    }

    /// <summary>
    /// List recent anti-cheat blocks (the per-team-user IP / fingerprint
    /// policy enforcement log). Newest first, capped at 200 rows.
    /// </summary>
    [RequireAdmin]
    [HttpGet("AntiCheatBlocks")]
    [ProducesResponseType(typeof(Models.Response.Admin.AntiCheatBlockModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAntiCheatBlocks(
        [FromServices] AppDbContext dbContext,
        [FromQuery][Range(1, 500)] int count = 100,
        [FromQuery] int skip = 0,
        CancellationToken token = default)
    {
        var rows = await dbContext.AntiCheatBlocks.AsNoTracking()
            .OrderByDescending(b => b.OccurredAtUtc)
            .Skip(skip).Take(count)
            .Select(b => new Models.Response.Admin.AntiCheatBlockModel
            {
                Id = b.Id,
                UserId = b.UserId,
                UserName = b.UserName,
                ConflictUserId = b.ConflictUserId,
                ConflictUserName = b.ConflictUserName,
                Kind = b.Kind,
                ConflictingValue = b.ConflictingValue,
                OccurredAtUtc = b.OccurredAtUtc
            })
            .ToArrayAsync(token);
        return Ok(rows);
    }

    /// <summary>
    /// Remove an anti-cheat block row. Useful when an admin determines
    /// a false positive (e.g., teammates legitimately share a NAT'd
    /// public IP). The block is purely advisory — deleting it does not
    /// retroactively allow the past login; it just drops the record.
    /// </summary>
    [RequireAdmin]
    [HttpDelete("AntiCheatBlocks/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearAntiCheatBlock(
        [FromRoute] int id, [FromServices] AppDbContext dbContext, CancellationToken token)
    {
        var row = await dbContext.AntiCheatBlocks.FirstOrDefaultAsync(b => b.Id == id, token);
        if (row is null) return NotFound(new RequestResponse("Block not found."));
        dbContext.AntiCheatBlocks.Remove(row);
        await dbContext.SaveChangesAsync(token);
        return Ok();
    }

    // =========================================================
    //  Challenge image build observability
    //  See /root/.claude/plans/compiled-squishing-neumann.md
    // =========================================================

    /// <summary>
    /// Paginated audit history across all challenge builds. Newest
    /// first. Supports filtering by status (Failed by default omitted —
    /// pass <c>status=</c> for the full history) and by game.
    /// </summary>
    [RequireAdmin]
    [HttpGet("Builds")]
    [ProducesResponseType(typeof(Models.Response.Admin.ChallengeBuildAuditModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListBuilds(
        [FromServices] AppDbContext dbContext,
        [FromQuery][Range(1, 500)] int count = 50,
        [FromQuery] int skip = 0,
        [FromQuery] ChallengeBuildStatus? status = null,
        [FromQuery] int? gameId = null,
        CancellationToken token = default)
    {
        var q = dbContext.ChallengeBuildAudits.AsNoTracking()
            .Include(a => a.Challenge)
            .OrderByDescending(a => a.EnqueuedAtUtc)
            .AsQueryable();
        if (status is { } s) q = q.Where(a => a.Status == s);
        if (gameId is { } g) q = q.Where(a => a.GameId == g);

        var rows = await q.Skip(skip).Take(count)
            .Select(a => new Models.Response.Admin.ChallengeBuildAuditModel
            {
                Id = a.Id,
                ChallengeId = a.ChallengeId,
                GameId = a.GameId,
                ChallengeTitle = a.Challenge != null ? a.Challenge.Title : string.Empty,
                EnqueuedAtUtc = a.EnqueuedAtUtc,
                StartedAtUtc = a.StartedAtUtc,
                FinishedAtUtc = a.FinishedAtUtc,
                Trigger = a.Trigger,
                Attempt = a.Attempt,
                Status = a.Status,
                Digest = a.Digest,
                LogTail = a.LogTail,
                ErrorMessage = a.ErrorMessage,
                DurationMs = a.DurationMs
            })
            .ToArrayAsync(token);
        return Ok(rows);
    }

    /// <summary>
    /// Live snapshot of builds currently being processed by a worker.
    /// In-memory only; cleared on app restart.
    /// </summary>
    [RequireAdmin]
    [HttpGet("Builds/InProgress")]
    [ProducesResponseType(typeof(Models.Response.Admin.ChallengeBuildInProgressModel[]), StatusCodes.Status200OK)]
    public IActionResult ListBuildsInProgress(
        [FromServices] Services.Container.Build.IChallengeBuildQueue buildQueue)
    {
        var rows = buildQueue.GetInProgress()
            .OrderByDescending(b => b.StartedAtUtc)
            .Select(b => new Models.Response.Admin.ChallengeBuildInProgressModel
            {
                AuditId = b.AuditId,
                ChallengeId = b.ChallengeId,
                GameId = b.GameId,
                Slug = b.Slug,
                Attempt = b.Attempt,
                Trigger = b.Trigger,
                StartedAtUtc = b.StartedAtUtc
            })
            .ToArray();
        return Ok(rows);
    }

    /// <summary>
    /// Bulk-rebuild every <c>Failed</c> / <c>MissingDockerfile</c>
    /// challenge in a game. Skips challenges with no persisted archive
    /// (registry-image or admin-created entries) and reports the
    /// count.
    /// </summary>
    [RequireAdmin]
    [HttpPost("Games/{gameId:int}/BulkRebuild")]
    [ProducesResponseType(typeof(Models.Response.Admin.BulkRebuildResultModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkRebuildFailed(
        [FromRoute] int gameId,
        [FromServices] AppDbContext dbContext,
        [FromServices] Storage.Interface.IBlobStorage storage,
        [FromServices] Services.Container.Build.IChallengeBuildQueue buildQueue,
        CancellationToken token)
    {
        var candidates = await dbContext.GameChallenges
            .Where(c => c.GameId == gameId
                        && (c.BuildStatus == ChallengeBuildStatus.Failed
                            || c.BuildStatus == ChallengeBuildStatus.MissingDockerfile))
            .ToListAsync(token);

        var result = new Models.Response.Admin.BulkRebuildResultModel();
        var msgs = new List<string>();

        foreach (var ch in candidates)
        {
            if (string.IsNullOrEmpty(ch.OriginalArchiveBlobPath))
            {
                result.Skipped++;
                msgs.Add($"{ch.Title}: no archive on file");
                continue;
            }
            if (!await storage.ExistsAsync(ch.OriginalArchiveBlobPath, token))
            {
                result.Skipped++;
                msgs.Add($"{ch.Title}: archive blob missing");
                continue;
            }

            // Extract → snapshot → enqueue, mirroring the single-shot
            // Rebuild endpoint. Failures here are isolated per
            // challenge: skip the one and keep going.
            var workDir = Path.Combine(Path.GetTempPath(), $"gzctf-bulk-{Guid.NewGuid():N}");
            try
            {
                Directory.CreateDirectory(workDir);
                await using (var src = await storage.OpenReadAsync(ch.OriginalArchiveBlobPath, token))
                {
                    var spool = Path.Combine(workDir, "__archive.bin");
                    await using (var fs = System.IO.File.Create(spool))
                        await src.CopyToAsync(fs, token);
                    await using var sf = System.IO.File.OpenRead(spool);
                    await Services.Transfer.ChallengeImportService.ExtractArchiveAsync(sf, workDir, token);
                    System.IO.File.Delete(spool);
                }

                var topLevel = Directory.EnumerateFileSystemEntries(workDir).Take(2).ToArray();
                var packageDir = topLevel.Length == 1 && Directory.Exists(topLevel[0])
                    ? topLevel[0]
                    : workDir;

                var srcDir = Path.Combine(packageDir, "src");
                string contextDir = System.IO.File.Exists(Path.Combine(srcDir, "Dockerfile"))
                    ? Path.GetFullPath(srcDir)
                    : Path.GetFullPath(packageDir);
                const string dockerfile = "Dockerfile";

                if (!System.IO.File.Exists(Path.Combine(contextDir, dockerfile)))
                {
                    result.Skipped++;
                    msgs.Add($"{ch.Title}: no Dockerfile in archive");
                    continue;
                }

                var snap = Path.Combine(Path.GetTempPath(), $"gzctf-build-{Guid.NewGuid():N}");
                CopyDirRecursive(contextDir, snap);

                ch.BuildStatus = ChallengeBuildStatus.Queued;
                ch.LastBuildLog = null;
                buildQueue.Enqueue(new Services.Container.Build.ChallengeBuildJob(
                    ch.Id, ch.GameId, ch.Title, snap, dockerfile,
                    BuildTrigger.Bulk));
                result.Enqueued++;
            }
            catch (Exception ex)
            {
                result.Skipped++;
                msgs.Add($"{ch.Title}: {ex.Message}");
            }
            finally
            {
                try { Directory.Delete(workDir, recursive: true); } catch { /* best effort */ }
            }
        }

        if (result.Enqueued > 0)
            await dbContext.SaveChangesAsync(token);

        result.Messages = msgs.ToArray();
        return Ok(result);
    }

    static void CopyDirRecursive(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var f in Directory.EnumerateFiles(src))
            System.IO.File.Copy(f, Path.Combine(dst, Path.GetFileName(f)));
        foreach (var d in Directory.EnumerateDirectories(src))
            CopyDirRecursive(d, Path.Combine(dst, Path.GetFileName(d)));
    }

    private IActionResult HandleIdentityError(IEnumerable<IdentityError> errors) =>
        BadRequest(new RequestResponse(errors.FirstOrDefault()?.Description ??
                                       localizer[nameof(Resources.Program.Identity_UnknownError)]));

    // =========================================================
    //  Global repo bindings — multi-event ".gzevent" discovery
    //  See /root/.claude/plans/compiled-squishing-neumann.md
    // =========================================================

    /// <summary>
    /// List configured repo bindings with their child games.
    /// </summary>
    [RequireAdmin]
    [HttpGet("RepoBindings")]
    [ProducesResponseType(typeof(Models.Request.Edit.RepoBindingInfoModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRepoBindings(
        [FromServices] AppDbContext dbContext, CancellationToken token)
    {
        var rows = await dbContext.GameRepoBindings.AsNoTracking()
            .OrderByDescending(b => b.CreatedAtUtc)
            .Select(b => new Models.Request.Edit.RepoBindingInfoModel
            {
                Id = b.Id,
                RepoUrl = b.RepoUrl,
                Ref = b.Ref,
                CreatedAtUtc = b.CreatedAtUtc,
                LastScanUtc = b.LastScanUtc,
                NextScanUtc = b.NextScanUtc,
                IntervalSeconds = b.IntervalSeconds,
                Status = b.Status,
                LastCommitSha = b.LastCommitSha,
                LastScanMessage = b.LastScanMessage,
                HasGitHubToken = b.GitHubTokenEncrypted != null,
                TokenStatus = b.TokenStatus,
                CurrentActivity = b.CurrentActivity,
                Games = dbContext.Games
                    .Where(g => g.RepoBindingId == b.Id)
                    .OrderBy(g => g.Title)
                    .Select(g => new Models.Request.Edit.RepoBindingGameSummary
                    {
                        Id = g.Id,
                        Title = g.Title,
                        EventManifestPath = g.EventManifestPath
                    })
                    .ToArray()
            })
            .ToArrayAsync(token);
        return Ok(rows);
    }

    /// <summary>
    /// Register a new repo and immediately scan it for .gzevent manifests.
    /// </summary>
    [RequireAdmin]
    [HttpPost("RepoBindings")]
    [ProducesResponseType(typeof(Models.Request.Edit.RepoBindingScanResultModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRepoBinding(
        [FromBody] Models.Request.Edit.RepoBindingCreateModel model,
        [FromServices] AppDbContext dbContext,
        [FromServices] IDataProtectionProvider dataProtectionProvider,
        [FromServices] Services.Transfer.RepoBindingDiscoveryService discovery,
        CancellationToken token)
    {
        if (!Services.Transfer.GitHubLocator.TryParse(model.RepoUrl, model.Ref, overrideSubpath: null, out _, out var err))
            return BadRequest(new RequestResponse(err ?? "Invalid github URL."));

        var normalizedUrl = model.RepoUrl.Trim();
        var existing = await dbContext.GameRepoBindings
            .FirstOrDefaultAsync(b => b.RepoUrl == normalizedUrl, token);
        if (existing is not null)
            return Conflict(new RequestResponse(
                $"This repository is already registered (binding id {existing.Id}). Delete or scan that one instead.",
                StatusCodes.Status409Conflict));

        var protector = dataProtectionProvider.CreateProtector(
            Services.Transfer.GameRepoBindingProtection.Purpose);

        var user = (await userManager.GetUserAsync(User))!;

        var clamped = Math.Clamp(model.IntervalSeconds, 60, 86400);
        var hasToken = !string.IsNullOrWhiteSpace(model.GitHubToken);
        var binding = new GameRepoBinding
        {
            RepoUrl = normalizedUrl,
            Ref = string.IsNullOrWhiteSpace(model.Ref) ? null : model.Ref.Trim(),
            GitHubTokenEncrypted = hasToken
                ? protector.Protect(model.GitHubToken!.Trim())
                : null,
            TokenStatus = hasToken ? TokenStatus.Ok : TokenStatus.NotConfigured,
            CreatedByUserId = user.Id,
            IntervalSeconds = clamped,
            Status = RepoWatchStatus.Active,
            // RunImmediately: leave NextScanUtc null so the next poller
            // tick (~30s) picks it up. Otherwise schedule the first run
            // a full interval out.
            NextScanUtc = model.RunImmediately ? null : DateTimeOffset.UtcNow.AddSeconds(clamped)
        };
        dbContext.GameRepoBindings.Add(binding);
        await dbContext.SaveChangesAsync(token);

        // Synchronous first scan when RunImmediately is true so the
        // admin gets immediate feedback. Otherwise the poller will pick
        // it up later.
        if (!model.RunImmediately)
            return Ok(new Models.Request.Edit.RepoBindingScanResultModel());

        var result = await discovery.ScanAsync(binding.Id, user.Id, token);
        // discovery.ScanAsync writes LastScanUtc but not NextScanUtc;
        // do that here so the poller doesn't double-scan within the
        // same interval window.
        await dbContext.GameRepoBindings
            .Where(b => b.Id == binding.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.NextScanUtc,
                DateTimeOffset.UtcNow.AddSeconds(clamped)), token);
        return Ok(new Models.Request.Edit.RepoBindingScanResultModel
        {
            GamesCreated = result.GamesCreated,
            GamesUpdated = result.GamesUpdated,
            ChallengesImported = result.ChallengesImported,
            ChallengesUpdated = result.ChallengesUpdated,
            Failures = result.Failures,
            Messages = result.Messages.ToArray()
        });
    }

    /// <summary>
    /// Update a binding's mutable fields. Every property is optional —
    /// null leaves the existing value alone; <c>GitHubToken</c> follows
    /// the established "" = clear / value = re-protect convention.
    /// Pausing a binding stops the background poller from re-scanning
    /// it without losing the configured interval or token.
    /// </summary>
    [RequireAdmin]
    [HttpPut("RepoBindings/{id:int}")]
    [ProducesResponseType(typeof(Models.Request.Edit.RepoBindingInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRepoBinding(
        [FromRoute] int id,
        [FromBody] Models.Request.Edit.RepoBindingUpdateModel model,
        [FromServices] AppDbContext dbContext,
        [FromServices] IDataProtectionProvider dataProtectionProvider,
        CancellationToken token)
    {
        var binding = await dbContext.GameRepoBindings.FirstOrDefaultAsync(b => b.Id == id, token);
        if (binding is null)
            return NotFound(new RequestResponse("Binding not found."));

        if (model.Ref is not null)
            binding.Ref = string.IsNullOrWhiteSpace(model.Ref) ? null : model.Ref.Trim();
        if (model.IntervalSeconds is { } iv)
            binding.IntervalSeconds = Math.Clamp(iv, 60, 86400);
        if (model.Status is { } st)
        {
            // Resuming from Paused: pull NextScanUtc to "now" so the
            // poller picks it up immediately instead of waiting out the
            // remainder of the previously-scheduled gap.
            if (binding.Status == RepoWatchStatus.Paused && st == RepoWatchStatus.Active)
                binding.NextScanUtc = null;
            binding.Status = st;
        }
        if (model.GitHubToken is not null)
        {
            var protector = dataProtectionProvider.CreateProtector(
                Services.Transfer.GameRepoBindingProtection.Purpose);
            if (string.IsNullOrWhiteSpace(model.GitHubToken))
            {
                binding.GitHubTokenEncrypted = null;
                binding.TokenStatus = TokenStatus.NotConfigured;
            }
            else
            {
                binding.GitHubTokenEncrypted = protector.Protect(model.GitHubToken.Trim());
                binding.TokenStatus = TokenStatus.Ok;
            }
        }
        await dbContext.SaveChangesAsync(token);

        return Ok(new Models.Request.Edit.RepoBindingInfoModel
        {
            Id = binding.Id,
            RepoUrl = binding.RepoUrl,
            Ref = binding.Ref,
            CreatedAtUtc = binding.CreatedAtUtc,
            LastScanUtc = binding.LastScanUtc,
            NextScanUtc = binding.NextScanUtc,
            IntervalSeconds = binding.IntervalSeconds,
            Status = binding.Status,
            LastCommitSha = binding.LastCommitSha,
            LastScanMessage = binding.LastScanMessage,
            HasGitHubToken = binding.GitHubTokenEncrypted != null,
            TokenStatus = binding.TokenStatus
        });
    }

    /// <summary>
    /// Return the most recent scan-history rows for a binding so the
    /// admin can see *which* manifests failed without diving into logs.
    /// Newest first; capped at 20 rows.
    /// </summary>
    [RequireAdmin]
    [HttpGet("RepoBindings/{id:int}/Scans")]
    [ProducesResponseType(typeof(Models.Request.Edit.RepoBindingScanHistoryModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRepoBindingScans(
        [FromRoute] int id, [FromServices] AppDbContext dbContext, CancellationToken token)
    {
        var rows = await dbContext.GameRepoBindingScans.AsNoTracking()
            .Where(s => s.BindingId == id)
            .OrderByDescending(s => s.RanAtUtc)
            .Take(20)
            .Select(s => new Models.Request.Edit.RepoBindingScanHistoryModel
            {
                Id = s.Id,
                RanAtUtc = s.RanAtUtc,
                CommitSha = s.CommitSha,
                GamesCreated = s.GamesCreated,
                GamesUpdated = s.GamesUpdated,
                ChallengesImported = s.ChallengesImported,
                ChallengesUpdated = s.ChallengesUpdated,
                Failures = s.Failures,
                Messages = s.Messages
            })
            .ToArrayAsync(token);
        return Ok(rows);
    }

    /// <summary>
    /// Trigger a re-scan of the binding now.
    /// </summary>
    [RequireAdmin]
    [HttpPost("RepoBindings/{id:int}/Scan")]
    [ProducesResponseType(typeof(Models.Request.Edit.RepoBindingScanResultModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> ScanRepoBinding(
        [FromRoute] int id,
        [FromServices] Services.Transfer.RepoBindingDiscoveryService discovery,
        [FromServices] AppDbContext dbContext,
        CancellationToken token)
    {
        var user = (await userManager.GetUserAsync(User))!;
        var result = await discovery.ScanAsync(id, user.Id, token);

        // Reset the poll gate so the background poller waits a full
        // interval before re-running. Without this an Admin "Scan now"
        // + a poller tick a few seconds later would double-scan.
        var iv = await dbContext.GameRepoBindings.AsNoTracking()
            .Where(b => b.Id == id).Select(b => (int?)b.IntervalSeconds)
            .FirstOrDefaultAsync(token) ?? 600;
        var clamped = Math.Clamp(iv, 60, 86400);
        await dbContext.GameRepoBindings
            .Where(b => b.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.NextScanUtc,
                DateTimeOffset.UtcNow.AddSeconds(clamped)), token);

        return Ok(new Models.Request.Edit.RepoBindingScanResultModel
        {
            GamesCreated = result.GamesCreated,
            GamesUpdated = result.GamesUpdated,
            ChallengesImported = result.ChallengesImported,
            ChallengesUpdated = result.ChallengesUpdated,
            Failures = result.Failures,
            Messages = result.Messages.ToArray()
        });
    }

    /// <summary>
    /// Remove a repo binding. Does NOT delete child games — admin handles those manually.
    /// </summary>
    [RequireAdmin]
    [HttpDelete("RepoBindings/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRepoBinding(
        [FromRoute] int id, [FromServices] AppDbContext dbContext, CancellationToken token)
    {
        var binding = await dbContext.GameRepoBindings.FirstOrDefaultAsync(b => b.Id == id, token);
        if (binding is null)
            return NotFound(new RequestResponse("Binding not found."));

        // Detach child games (don't cascade-delete them).
        var children = await dbContext.Games.Where(g => g.RepoBindingId == id).ToListAsync(token);
        foreach (var g in children)
        {
            g.RepoBindingId = null;
            g.EventManifestPath = null;
        }

        dbContext.GameRepoBindings.Remove(binding);
        await dbContext.SaveChangesAsync(token);
        return Ok();
    }
}
