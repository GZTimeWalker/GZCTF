using System.Net.Mime;
using GZCTF.Extensions;
using GZCTF.Middlewares;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Account;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace GZCTF.Controllers;

/// <summary>
/// 用户账户相关接口
/// </summary>
[ApiController]
[Route("api/[controller]/[action]")]
[Produces(MediaTypeNames.Application.Json)]
public class AccountController(
    IMailSender mailSender,
    IFileRepository fileService,
    IHostEnvironment environment,
    ICaptchaExtension captcha,
    IOptionsSnapshot<AccountPolicy> accountPolicy,
    IOptionsSnapshot<GlobalConfig> globalConfig,
    UserManager<UserInfo> userManager,
    SignInManager<UserInfo> signInManager,
    ILogger<AccountController> logger,
    IStringLocalizer<Program> localizer) : ControllerBase
{
    /// <summary>
    /// 用户注册接口
    /// </summary>
    /// <remarks>
    /// 使用此接口注册新用户，Dev 环境下不校验，邮件URL：/verify
    /// </remarks>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">注册成功</response>
    /// <response code="400">校验失败或用户已存在</response>
    [HttpPost]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Register))]
    [ProducesResponseType(typeof(RequestResponse<RegisterStatus>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterModel model, CancellationToken token = default)
    {
        if (!accountPolicy.Value.AllowRegister)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_RegisterNotEnabled)]));

        if (accountPolicy.Value.UseCaptcha && !await captcha.VerifyAsync(model, HttpContext, token))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_TokenValidationFailed)]));

        if (!VerifyEmailDomain(model.Email))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_AvailableEmailDomain),
                accountPolicy.Value.EmailDomainList]));

        var user = new UserInfo { UserName = model.UserName, Email = model.Email, Role = Role.User };

        user.UpdateByHttpContext(HttpContext);

        IdentityResult result = await userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            UserInfo? current = await userManager.FindByEmailAsync(model.Email);

            if (current is null)
                return HandleIdentityError(result.Errors);

            if (await userManager.IsEmailConfirmedAsync(current))
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_UserExisting)]));

            user = current;
        }

        if (accountPolicy.Value.ActiveOnRegister)
        {
            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);
            await signInManager.SignInAsync(user, true);

            logger.Log(Program.StaticLocalizer[nameof(Resources.Program.Account_UserRegisteredLog)], user,
                TaskStatus.Success);
            return Ok(new RequestResponse<RegisterStatus>(localizer[nameof(Resources.Program.Account_UserRegistered)],
                RegisterStatus.LoggedIn,
                StatusCodes.Status200OK));
        }

        if (!accountPolicy.Value.EmailConfirmationRequired)
        {
            logger.Log(Program.StaticLocalizer[nameof(Resources.Program.Account_UserRegisteredWaitingApprovalLog)],
                user, TaskStatus.Success);
            return Ok(new RequestResponse<RegisterStatus>(
                localizer[nameof(Resources.Program.Account_UserRegisteredWaitingApproval)],
                RegisterStatus.AdminConfirmationRequired, StatusCodes.Status200OK));
        }

        logger.Log(Program.StaticLocalizer[nameof(Resources.Program.Account_SendEmailVerification)], user,
            TaskStatus.Pending);

        var rToken = Codec.Base64.Encode(await userManager.GenerateEmailConfirmationTokenAsync(user));
        var link = GetEmailLink("verify", rToken, model.Email);

        if (environment.IsDevelopment())
        {
            logger.Log(link, user, TaskStatus.Pending, LogLevel.Debug);
        }
        else
        {
            if (!mailSender.SendConfirmEmailUrl(user.UserName, user.Email, link, localizer, globalConfig))
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_EmailSendFailed)]));
        }

        return Ok(new RequestResponse<RegisterStatus>(
            localizer[nameof(Resources.Program.Account_UserRegisteredWaitingEmailVerification)],
            RegisterStatus.EmailConfirmationRequired, StatusCodes.Status200OK));
    }

    bool VerifyEmailDomain(string email)
    {
        var mailDomain = email.Split('@')[1];

        return string.IsNullOrWhiteSpace(accountPolicy.Value.EmailDomainList)
               || accountPolicy.Value.EmailDomainList
                   .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                   .Any(d => d.Equals(mailDomain, StringComparison.InvariantCulture));
    }

    /// <summary>
    /// 用户找回密码请求接口
    /// </summary>
    /// <remarks>
    /// 使用此接口请求找回密码，向用户邮箱发送邮件，邮件URL：/reset
    /// </remarks>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">用户密码重置邮件发送成功</response>
    /// <response code="400">校验失败</response>
    /// <response code="404">用户不存在</response>
    [HttpPost]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Register))]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Recovery([FromBody] RecoveryModel model, CancellationToken token = default)
    {
        if (accountPolicy.Value.UseCaptcha && !await captcha.VerifyAsync(model, HttpContext, token))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_TokenValidationFailed)]));

        UserInfo? user = await userManager.FindByEmailAsync(model.Email!);
        if (user is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Account_UserNotExist)],
                StatusCodes.Status404NotFound));

        if (!user.EmailConfirmed)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Account_EmailNotConfirmed)],
                StatusCodes.Status404NotFound));

        if (!accountPolicy.Value.EmailConfirmationRequired)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_ResetPasswordFromAdmin)]));

        logger.Log(Program.StaticLocalizer[nameof(Resources.Program.Account_SendEmailVerification)], HttpContext,
            TaskStatus.Pending);

        var rToken = Codec.Base64.Encode(await userManager.GeneratePasswordResetTokenAsync(user));
        var link = GetEmailLink("reset", rToken, model.Email);

        if (environment.IsDevelopment())
        {
            logger.Log(link, user, TaskStatus.Pending, LogLevel.Debug);
        }
        else
        {
            if (!mailSender.SendResetPasswordUrl(user.UserName, user.Email, link, localizer, globalConfig))
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_EmailSendFailed)]));
        }

        return Ok(new RequestResponse(localizer[nameof(Resources.Program.Account_EmailSent)], StatusCodes.Status200OK));
    }

    /// <summary>
    /// 用户重置密码接口
    /// </summary>
    /// <remarks>
    /// 使用此接口重置密码，需要邮箱验证码
    /// </remarks>
    /// <param name="model"></param>
    /// <response code="200">用户成功重置密码</response>
    /// <response code="400">校验失败</response>
    [HttpPost]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Register))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PasswordReset([FromBody] PasswordResetModel model)
    {
        UserInfo? user = await userManager.FindByEmailAsync(Codec.Base64.Decode(model.Email));
        if (user is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_InvalidEmail)]));

        user.UpdateByHttpContext(HttpContext);

        IdentityResult result =
            await userManager.ResetPasswordAsync(user, Codec.Base64.Decode(model.RToken), model.Password);

        if (!result.Succeeded)
            return HandleIdentityError(result.Errors);

        logger.Log(Program.StaticLocalizer[nameof(Resources.Program.Account_PasswordReset)], user, TaskStatus.Success);

        return Ok();
    }

    /// <summary>
    /// 用户邮箱确认接口
    /// </summary>
    /// <remarks>
    /// 使用此接口通过邮箱验证码确认邮箱
    /// </remarks>
    /// <param name="model"></param>
    /// <response code="200">用户通过邮箱验证</response>
    /// <response code="400">校验失败</response>
    /// <response code="401">邮箱验证失败</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Verify([FromBody] AccountVerifyModel model)
    {
        UserInfo? user = await userManager.FindByEmailAsync(Codec.Base64.Decode(model.Email));

        if (user is null || user.EmailConfirmed)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_InvalidEmail)]));

        IdentityResult result = await userManager.ConfirmEmailAsync(user, Codec.Base64.Decode(model.Token));

        if (!result.Succeeded)
            return Unauthorized(new RequestResponse(
                localizer[nameof(Resources.Program.Account_EmailVerificationFailed)],
                StatusCodes.Status401Unauthorized));

        logger.Log(Program.StaticLocalizer[nameof(Resources.Program.Account_EmailVerified)], user, TaskStatus.Success);
        await signInManager.SignInAsync(user, true);

        user.LastSignedInUtc = DateTimeOffset.UtcNow;
        user.LastVisitedUtc = DateTimeOffset.UtcNow;
        user.RegisterTimeUtc = DateTimeOffset.UtcNow;

        result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return HandleIdentityError(result.Errors);

        return Ok();
    }

    /// <summary>
    /// 用户登录接口
    /// </summary>
    /// <remarks>
    /// 使用此接口登录账户
    /// </remarks>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">用户成功登录</response>
    /// <response code="400">校验失败</response>
    /// <response code="401">用户名或密码错误</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LogIn([FromBody] LoginModel model, CancellationToken token = default)
    {
        if (accountPolicy.Value.UseCaptcha && !await captcha.VerifyAsync(model, HttpContext, token))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_TokenValidationFailed)]));

        UserInfo? user = await userManager.FindByNameAsync(model.UserName);
        user ??= await userManager.FindByEmailAsync(model.UserName);

        if (user is null)
            return Unauthorized(new RequestResponse(
                localizer[nameof(Resources.Program.Account_IncorrectUserNameOrPassword)],
                StatusCodes.Status401Unauthorized));

        if (user.Role == Role.Banned)
            return Unauthorized(new RequestResponse(localizer[nameof(Resources.Program.Account_UserDisabled)],
                StatusCodes.Status401Unauthorized));

        user.LastSignedInUtc = DateTimeOffset.UtcNow;
        user.UpdateByHttpContext(HttpContext);

        await signInManager.SignOutAsync();

        SignInResult result = await signInManager.PasswordSignInAsync(user, model.Password, true, false);

        if (!result.Succeeded)
            return Unauthorized(new RequestResponse(
                localizer[nameof(Resources.Program.Account_IncorrectUserNameOrPassword)],
                StatusCodes.Status401Unauthorized));

        logger.Log(Program.StaticLocalizer[nameof(Resources.Program.Account_UserLogined)], user, TaskStatus.Success);

        return Ok();
    }

    /// <summary>
    /// 用户登出接口
    /// </summary>
    /// <remarks>
    /// 使用此接口登出账户，需要User权限
    /// </remarks>
    /// <response code="200">用户已登出</response>
    /// <response code="401">无权访问</response>
    [HttpPost]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> LogOut()
    {
        await signInManager.SignOutAsync();

        return Ok();
    }

    /// <summary>
    /// 用户数据更新接口
    /// </summary>
    /// <remarks>
    /// 使用此接口更新用户用户名和描述，需要User权限
    /// </remarks>
    /// <param name="model"></param>
    /// <response code="200">用户数据成功更新</response>
    /// <response code="400">校验失败或用户数据更新失败</response>
    /// <response code="401">无权访问</response>
    [HttpPut]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromBody] ProfileUpdateModel model)
    {
        UserInfo? user = await userManager.GetUserAsync(User);

        if (model.UserName is not null && model.UserName != user!.UserName)
        {
            var oldName = user.UserName;

            IdentityResult unameRes = await userManager.SetUserNameAsync(user, model.UserName);

            if (!unameRes.Succeeded)
                return HandleIdentityError(unameRes.Errors);

            logger.Log(Program.StaticLocalizer[nameof(Resources.Program.Account_UserUpdated), oldName!, user.UserName!],
                user, TaskStatus.Success);
        }

        user!.UpdateUserInfo(model);
        IdentityResult result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return HandleIdentityError(result.Errors);

        return Ok();
    }

    /// <summary>
    /// 用户密码更改接口
    /// </summary>
    /// <remarks>
    /// 使用此接口更新用户密码，需要User权限
    /// </remarks>
    /// <param name="model"></param>
    /// <response code="200">用户成功更新密码</response>
    /// <response code="400">校验失败或用户密码更新失败</response>
    /// <response code="401">无权访问</response>
    [HttpPut]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] PasswordChangeModel model)
    {
        UserInfo? user = await userManager.GetUserAsync(User);
        IdentityResult result = await userManager.ChangePasswordAsync(user!, model.Old, model.New);

        if (!result.Succeeded)
            return HandleIdentityError(result.Errors);

        logger.Log(Program.StaticLocalizer[nameof(Resources.Program.Account_PasswordChanged)], user,
            TaskStatus.Success);

        return Ok();
    }

    /// <summary>
    /// 用户邮箱更改接口
    /// </summary>
    /// <remarks>
    /// 使用此接口更改用户邮箱，需要User权限，邮件URL：/confirm
    /// </remarks>
    /// <param name="model"></param>
    /// <response code="200">成功发送用户邮箱更改邮件，布尔值表示是否需要邮箱验证</response>
    /// <response code="400">校验失败或邮箱已经被占用</response>
    /// <response code="401">无权访问</response>
    [HttpPut]
    [RequireUser]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Register))]
    [ProducesResponseType(typeof(RequestResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangeEmail([FromBody] MailChangeModel model)
    {
        if (await userManager.FindByEmailAsync(model.NewMail) is not null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_EmailUsed)]));

        if (!VerifyEmailDomain(model.NewMail))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_AvailableEmailDomain),
                accountPolicy.Value.EmailDomainList]));

        UserInfo? user = await userManager.GetUserAsync(User);

        if (!accountPolicy.Value.EmailConfirmationRequired)
            return BadRequest(
                new RequestResponse<bool>(localizer[nameof(Resources.Program.Account_ChangeEmailFromAdmin)], false));

        logger.Log(Program.StaticLocalizer[nameof(Resources.Program.Account_SendEmailChange)], user,
            TaskStatus.Pending);

        var token = Codec.Base64.Encode(await userManager.GenerateChangeEmailTokenAsync(user!, model.NewMail));
        var link = GetEmailLink("confirm", token, model.NewMail);

        if (environment.IsDevelopment())
        {
            logger.Log(link, user, TaskStatus.Pending, LogLevel.Debug);
        }
        else
        {
            if (!mailSender.SendChangeEmailUrl(user!.UserName, model.NewMail, link, localizer, globalConfig))
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_EmailSendFailed)]));
        }

        return Ok(new RequestResponse<bool>(localizer[nameof(Resources.Program.Account_EmailVerificationPending)], true,
            StatusCodes.Status200OK));
    }

    /// <summary>
    /// 用户邮箱更改确认接口
    /// </summary>
    /// <remarks>
    /// 使用此接口确认更改用户邮箱，需要邮箱验证码，需要User权限
    /// </remarks>
    /// <param name="model"></param>
    /// <response code="200">用户成功更改邮箱</response>
    /// <response code="400">校验失败或无效邮箱</response>
    /// <response code="401">未授权用户</response>
    /// <response code="403">无权访问</response>
    [HttpPost]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MailChangeConfirm([FromBody] AccountVerifyModel model)
    {
        UserInfo? user = await userManager.GetUserAsync(User);
        IdentityResult result = await userManager.ChangeEmailAsync(user!, Codec.Base64.Decode(model.Email),
            Codec.Base64.Decode(model.Token));

        if (!result.Succeeded)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_InvalidEmail)]));

        logger.Log(Program.StaticLocalizer[nameof(Resources.Program.Account_EmailChanged)], user, TaskStatus.Success);

        return Ok();
    }

    /// <summary>
    /// 获取用户信息接口
    /// </summary>
    /// <remarks>
    /// 使用此接口获取用户信息，需要User权限
    /// </remarks>
    /// <response code="200">用户成功获取信息</response>
    /// <response code="401">未授权用户</response>
    [HttpGet]
    [RequireUser]
    [ProducesResponseType(typeof(ProfileUserInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Profile()
    {
        UserInfo? user = await userManager.GetUserAsync(User);

        return Ok(ProfileUserInfoModel.FromUserInfo(user!));
    }

    /// <summary>
    /// 更新用户头像接口
    /// </summary>
    /// <remarks>
    /// 使用此接口更新用户头像，需要User权限
    /// </remarks>
    /// <response code="200">用户头像URL</response>
    /// <response code="400">非法请求</response>
    /// <response code="401">未授权用户</response>
    [HttpPut]
    [RequireUser]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Avatar(IFormFile file, CancellationToken token)
    {
        if (file.Length == 0)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_SizeZero)]));

        if (file.Length > 3 * 1024 * 1024)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_SizeTooLarge)]));

        UserInfo? user = await userManager.GetUserAsync(User);

        if (user!.AvatarHash is not null)
            await fileService.DeleteFileByHash(user.AvatarHash, token);

        LocalFile? avatar = await fileService.CreateOrUpdateImage(file, "avatar", 300, token);

        if (avatar is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Avatar_UpdateFailed)]));

        user.AvatarHash = avatar.Hash;
        IdentityResult result = await userManager.UpdateAsync(user);

        if (result != IdentityResult.Success)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_UserUpdateFailed)]));

        logger.Log(Program.StaticLocalizer[nameof(Resources.Program.Account_AvatarUpdated), avatar.Hash[..8]], user,
            TaskStatus.Success);

        return Ok(avatar.Url());
    }

    string GetEmailLink(string action, string token, string? email)
        => $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/account/{action}?" +
           $"token={token}&email={Codec.Base64.Encode(email)}";

    BadRequestObjectResult HandleIdentityError(IEnumerable<IdentityError> errors) =>
        BadRequest(new RequestResponse(errors.FirstOrDefault()?.Description ??
                                       localizer[nameof(Resources.Program.Identity_UnknownError)]));
}
