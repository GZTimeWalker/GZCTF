using System.Net.Mime;
using GZCTF.Middlewares;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Account;
using GZCTF.Repositories.Interface;
using GZCTF.Services;
using GZCTF.Services.Config;
using GZCTF.Services.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace GZCTF.Controllers;

/// <summary>
/// User account related APIs
/// </summary>
[ApiController]
[Route("api/[controller]/[action]")]
[Produces(MediaTypeNames.Application.Json)]
public class AccountController(
    IMailSender mailSender,
    IBlobRepository blobService,
    IHostEnvironment environment,
    ICaptchaService captcha,
    IConfigService configService,
    IOptionsSnapshot<AccountPolicy> accountPolicy,
    IOptionsSnapshot<GlobalConfig> globalConfig,
    UserManager<UserInfo> userManager,
    SignInManager<UserInfo> signInManager,
    ILogger<AccountController> logger,
    IStringLocalizer<Program> localizer) : ControllerBase
{
    /// <summary>
    /// User registration
    /// </summary>
    /// <remarks>
    /// Use this API to register a new user. In development environment, no verification. Email URL: /verify
    /// </remarks>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">Registration successful</response>
    /// <response code="400">Validation failed or user already exists</response>
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

        var password = configService.DecryptApiData(model.Password);
        if (string.IsNullOrWhiteSpace(password))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Model_PasswordRequired)]));

        var user = new UserInfo { UserName = model.UserName, Email = model.Email, Role = Role.User };

        user.UpdateByHttpContext(HttpContext);

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var current = await userManager.FindByEmailAsync(model.Email);

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

            logger.Log(StaticLocalizer[nameof(Resources.Program.Account_UserRegisteredLog)], user,
                TaskStatus.Success);
            return Ok(new RequestResponse<RegisterStatus>(localizer[nameof(Resources.Program.Account_UserRegistered)],
                RegisterStatus.LoggedIn,
                StatusCodes.Status200OK));
        }

        if (!accountPolicy.Value.EmailConfirmationRequired)
        {
            logger.Log(StaticLocalizer[nameof(Resources.Program.Account_UserRegisteredWaitingApprovalLog)],
                user, TaskStatus.Success);
            return Ok(new RequestResponse<RegisterStatus>(
                localizer[nameof(Resources.Program.Account_UserRegisteredWaitingApproval)],
                RegisterStatus.AdminConfirmationRequired, StatusCodes.Status200OK));
        }

        logger.Log(StaticLocalizer[nameof(Resources.Program.Account_SendEmailVerification)], user,
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
    /// User password recovery request
    /// </summary>
    /// <remarks>
    /// Use this API to request password recovery. Sends an email to the user. Email URL: /reset
    /// </remarks>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">Password reset email sent successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">User does not exist</response>
    [HttpPost]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Register))]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Recovery([FromBody] RecoveryModel model, CancellationToken token = default)
    {
        if (accountPolicy.Value.UseCaptcha && !await captcha.VerifyAsync(model, HttpContext, token))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_TokenValidationFailed)]));

        var user = await userManager.FindByEmailAsync(model.Email!);
        if (user is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Account_UserNotExist)],
                StatusCodes.Status404NotFound));

        if (!user.EmailConfirmed)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Account_EmailNotConfirmed)],
                StatusCodes.Status404NotFound));

        if (!accountPolicy.Value.EmailConfirmationRequired)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_ResetPasswordFromAdmin)]));

        logger.Log(StaticLocalizer[nameof(Resources.Program.Account_SendEmailVerification)], HttpContext,
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
    /// User password reset
    /// </summary>
    /// <remarks>
    /// Use this API to reset the password. Email verification code is required.
    /// </remarks>
    /// <param name="model"></param>
    /// <response code="200">Password reset successfully</response>
    /// <response code="400">Validation failed</response>
    [HttpPost]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Register))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PasswordReset([FromBody] PasswordResetModel model)
    {
        var password = configService.DecryptApiData(model.Password);
        if (string.IsNullOrWhiteSpace(password))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Model_PasswordRequired)]));

        var user = await userManager.FindByEmailAsync(Codec.Base64.Decode(model.Email));
        if (user is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_InvalidEmail)]));

        user.UpdateByHttpContext(HttpContext);

        var token = Codec.Base64.Decode(model.RToken);
        var result = await userManager.ResetPasswordAsync(user, token, password);

        if (!result.Succeeded)
            return HandleIdentityError(result.Errors);

        logger.Log(StaticLocalizer[nameof(Resources.Program.Account_PasswordReset)], user, TaskStatus.Success);

        return Ok();
    }

    /// <summary>
    /// User email confirmation
    /// </summary>
    /// <remarks>
    /// Use this API to confirm email using the verification code.
    /// </remarks>
    /// <param name="model"></param>
    /// <response code="200">Email verified successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="401">Email verification failed</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Verify([FromBody] AccountVerifyModel model)
    {
        var user = await userManager.FindByEmailAsync(Codec.Base64.Decode(model.Email));

        if (user is null || user.EmailConfirmed)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_InvalidEmail)]));

        var result = await userManager.ConfirmEmailAsync(user, Codec.Base64.Decode(model.Token));

        if (!result.Succeeded)
            return Unauthorized(new RequestResponse(
                localizer[nameof(Resources.Program.Account_EmailVerificationFailed)],
                StatusCodes.Status401Unauthorized));

        logger.Log(StaticLocalizer[nameof(Resources.Program.Account_EmailVerified)], user, TaskStatus.Success);
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
    /// User login
    /// </summary>
    /// <remarks>
    /// Use this API to log in to the account.
    /// </remarks>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">Login successful</response>
    /// <response code="400">Validation failed</response>
    /// <response code="401">Incorrect username or password</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LogIn([FromBody] LoginModel model, CancellationToken token = default)
    {
        if (accountPolicy.Value.UseCaptcha && !await captcha.VerifyAsync(model, HttpContext, token))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_TokenValidationFailed)]));

        var password = configService.DecryptApiData(model.Password);
        if (string.IsNullOrWhiteSpace(password))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Model_PasswordRequired)]));

        var user = await userManager.FindByNameAsync(model.UserName);
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
        var result = await signInManager.PasswordSignInAsync(user, password, true, false);

        if (!result.Succeeded)
            return Unauthorized(new RequestResponse(
                localizer[nameof(Resources.Program.Account_IncorrectUserNameOrPassword)],
                StatusCodes.Status401Unauthorized));

        logger.Log(StaticLocalizer[nameof(Resources.Program.Account_UserLogined)], user, TaskStatus.Success);

        return Ok();
    }

    /// <summary>
    /// User logout
    /// </summary>
    /// <remarks>
    /// Use this API to log out of the account. User permissions required.
    /// </remarks>
    /// <response code="200">Logged out successfully</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> LogOut()
    {
        await signInManager.SignOutAsync();

        return Ok();
    }

    /// <summary>
    /// User data update
    /// </summary>
    /// <remarks>
    /// Use this API to update username and description. User permissions required.
    /// </remarks>
    /// <param name="model"></param>
    /// <response code="200">User data updated successfully</response>
    /// <response code="400">Validation failed or user data update failed</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromBody] ProfileUpdateModel model)
    {
        var user = await userManager.GetUserAsync(User);

        if (model.UserName is not null && model.UserName != user!.UserName)
        {
            var oldName = user.UserName;

            var unameRes = await userManager.SetUserNameAsync(user, model.UserName);

            if (!unameRes.Succeeded)
                return HandleIdentityError(unameRes.Errors);

            logger.Log(StaticLocalizer[nameof(Resources.Program.Account_UserUpdated), oldName!, user.UserName!],
                user, TaskStatus.Success);
        }

        user!.UpdateUserInfo(model);
        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return HandleIdentityError(result.Errors);

        return Ok();
    }

    /// <summary>
    /// User password change
    /// </summary>
    /// <remarks>
    /// Use this API to change user's password. User permissions required.
    /// </remarks>
    /// <param name="model"></param>
    /// <response code="200">Password changed successfully</response>
    /// <response code="400">Validation failed or password change failed</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] PasswordChangeModel model)
    {
        var user = await userManager.GetUserAsync(User);

        var oldPassword = configService.DecryptApiData(model.Old);
        if (string.IsNullOrWhiteSpace(oldPassword))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Model_OldPasswordRequired)]));

        var newPassword = configService.DecryptApiData(model.New);
        if (string.IsNullOrWhiteSpace(newPassword))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Model_NewPasswordRequired)]));

        var result = await userManager.ChangePasswordAsync(user!, oldPassword, newPassword);

        if (!result.Succeeded)
            return HandleIdentityError(result.Errors);

        logger.Log(StaticLocalizer[nameof(Resources.Program.Account_PasswordChanged)], user,
            TaskStatus.Success);

        return Ok();
    }

    /// <summary>
    /// User email change
    /// </summary>
    /// <remarks>
    /// Use this API to change user's email. User permissions required. Email URL: /confirm
    /// </remarks>
    /// <param name="model"></param>
    /// <response code="200">Email change email sent successfully. Boolean indicates whether email verification is required</response>
    /// <response code="400">Validation failed or email already in use</response>
    /// <response code="401">Unauthorized</response>
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

        var user = await userManager.GetUserAsync(User);

        if (!accountPolicy.Value.EmailConfirmationRequired)
            return BadRequest(
                new RequestResponse<bool>(localizer[nameof(Resources.Program.Account_ChangeEmailFromAdmin)], false));

        logger.Log(StaticLocalizer[nameof(Resources.Program.Account_SendEmailChange)], user,
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
    /// User email change confirmation
    /// </summary>
    /// <remarks>
    /// Use this API to confirm email change. Email verification code required. User permissions required.
    /// </remarks>
    /// <param name="model"></param>
    /// <response code="200">Email changed successfully</response>
    /// <response code="400">Validation failed or invalid email</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [HttpPost]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MailChangeConfirm([FromBody] AccountVerifyModel model)
    {
        var user = await userManager.GetUserAsync(User);
        var result = await userManager.ChangeEmailAsync(user!, Codec.Base64.Decode(model.Email),
            Codec.Base64.Decode(model.Token));

        if (!result.Succeeded)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_InvalidEmail)]));

        logger.Log(StaticLocalizer[nameof(Resources.Program.Account_EmailChanged)], user, TaskStatus.Success);

        return Ok();
    }

    /// <summary>
    /// Get user information
    /// </summary>
    /// <remarks>
    /// Use this API to get user information. User permissions required.
    /// </remarks>
    /// <response code="200">User information retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [RequireUser]
    [ProducesResponseType(typeof(ProfileUserInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Profile()
    {
        var user = await userManager.GetUserAsync(User);

        return Ok(ProfileUserInfoModel.FromUserInfo(user!));
    }

    /// <summary>
    /// Update user avatar
    /// </summary>
    /// <remarks>
    /// Use this API to update user's avatar. User permissions required.
    /// </remarks>
    /// <response code="200">User avatar URL</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut]
    [RequireUser]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Avatar(IFormFile file, CancellationToken token)
    {
        switch (file.Length)
        {
            case 0:
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_SizeZero)]));
            case > 3 * 1024 * 1024:
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_SizeTooLarge)]));
        }

        var user = await userManager.GetUserAsync(User);

        if (user!.AvatarHash is not null)
            await blobService.DeleteBlobByHash(user.AvatarHash, token);

        var avatar = await blobService.CreateOrUpdateImage(file, "avatar", 300, token);

        if (avatar is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Avatar_UpdateFailed)]));

        user.AvatarHash = avatar.Hash;
        var result = await userManager.UpdateAsync(user);

        if (result != IdentityResult.Success)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_UserUpdateFailed)]));

        logger.Log(StaticLocalizer[nameof(Resources.Program.Account_AvatarUpdated), avatar.Hash[..8]], user,
            TaskStatus.Success);

        return Ok(avatar.Url());
    }

    string GetEmailLink(string action, string token, string? email)
        => $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/account/{action}?" +
           $"token={token}&email={Codec.Base64.Encode(email)}";

    BadRequestObjectResult HandleIdentityError(IEnumerable<IdentityError> errors) =>
        BadRequest(new RequestResponse(errors.FirstOrDefault()?.Description ??
                                       localizer[nameof(Resources.Program.Identity_UnknownError)]));

    #region Passkey

    /// <summary>
    /// Get passkey attestation options for registration
    /// </summary>
    /// <remarks>
    /// Use this API to get options for creating a new passkey. User permissions required.
    /// The response should be passed to navigator.credentials.create() in the browser.
    /// </remarks>
    /// <param name="token">Cancellation token</param>
    /// <response code="200">Passkey attestation options (JSON)</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost]
    [RequireUser]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PasskeyAttestationOptions(CancellationToken token = default)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized(new RequestResponse(localizer[nameof(Resources.Program.Account_UserNotExist)],
                StatusCodes.Status401Unauthorized));

        var userEntity = new PasskeyUserEntity
        {
            Id = user.Id.ToString(),
            Name = user.UserName ?? user.Email ?? user.Id.ToString(),
            DisplayName = user.UserName ?? user.Email ?? user.Id.ToString()
        };

        var optionsJson = await signInManager.MakePasskeyCreationOptionsAsync(userEntity);

        return Content(optionsJson, "application/json");
    }

    /// <summary>
    /// Complete passkey registration
    /// </summary>
    /// <remarks>
    /// Use this API to complete passkey registration with the credential from navigator.credentials.create().
    /// User permissions required.
    /// </remarks>
    /// <param name="model">Passkey attestation data</param>
    /// <param name="token">Cancellation token</param>
    /// <response code="200">Passkey registered successfully</response>
    /// <response code="400">Invalid passkey data</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PasskeyAttestation(
        [FromBody] PasskeyAttestationRequest model,
        CancellationToken token = default)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized(new RequestResponse(localizer[nameof(Resources.Program.Account_UserNotExist)],
                StatusCodes.Status401Unauthorized));

        var result = await signInManager.PerformPasskeyAttestationAsync(model.CredentialJson);

        if (!result.Succeeded)
        {
            logger.Log(StaticLocalizer[nameof(Resources.Program.Passkey_RegistrationFailed)], user,
                TaskStatus.Failed);
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Passkey_RegistrationFailed)]));
        }

        // Set passkey name if provided
        if (!string.IsNullOrEmpty(model.Name))
            result.Passkey.Name = model.Name;

        // Store the passkey
        var storeResult = await userManager.AddOrUpdatePasskeyAsync(user, result.Passkey);
        if (!storeResult.Succeeded)
        {
            logger.Log(StaticLocalizer[nameof(Resources.Program.Passkey_RegistrationFailed)], user,
                TaskStatus.Failed);
            return HandleIdentityError(storeResult.Errors);
        }

        logger.Log(StaticLocalizer[nameof(Resources.Program.Passkey_Registered)], user, TaskStatus.Success);

        return Ok();
    }

    /// <summary>
    /// Get passkey assertion options for login
    /// </summary>
    /// <remarks>
    /// Use this API to get options for authenticating with a passkey.
    /// The response should be passed to navigator.credentials.get() in the browser.
    /// </remarks>
    /// <param name="model">Optional username to scope allowed credentials</param>
    /// <param name="token">Cancellation token</param>
    /// <response code="200">Passkey assertion options (JSON)</response>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> PasskeyAssertionOptions(
        [FromBody] PasskeyAssertionOptionsRequest model,
        CancellationToken token = default)
    {
        UserInfo? user = null;
        if (!string.IsNullOrEmpty(model.UserName))
        {
            user = await userManager.FindByNameAsync(model.UserName);
            user ??= await userManager.FindByEmailAsync(model.UserName);
        }

        var optionsJson = await signInManager.MakePasskeyRequestOptionsAsync(user);
        return Content(optionsJson, "application/json");
    }

    /// <summary>
    /// Complete passkey authentication
    /// </summary>
    /// <remarks>
    /// Use this API to complete passkey authentication with the credential from navigator.credentials.get().
    /// </remarks>
    /// <param name="model">Passkey assertion data</param>
    /// <param name="token">Cancellation token</param>
    /// <response code="200">Login successful</response>
    /// <response code="400">Invalid passkey data</response>
    /// <response code="401">Authentication failed</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PasskeyAssertion(
        [FromBody] PasskeyAssertionRequest model,
        CancellationToken token = default)
    {
        var result = await signInManager.PasskeySignInAsync(model.CredentialJson);

        if (!result.Succeeded)
        {
            logger.Log(StaticLocalizer[nameof(Resources.Program.Passkey_LoginFailed)], HttpContext,
                TaskStatus.Failed);
            return Unauthorized(new RequestResponse(localizer[nameof(Resources.Program.Passkey_LoginFailed)],
                StatusCodes.Status401Unauthorized));
        }

        // Get the signed-in user for logging
        var user = await userManager.GetUserAsync(User);
        if (user is not null)
        {
            // Check if user is banned
            if (user.Role == Role.Banned)
            {
                await signInManager.SignOutAsync();
                return Unauthorized(new RequestResponse(localizer[nameof(Resources.Program.Account_UserDisabled)],
                    StatusCodes.Status401Unauthorized));
            }

            user.LastSignedInUtc = DateTimeOffset.UtcNow;
            user.UpdateByHttpContext(HttpContext);
            await userManager.UpdateAsync(user);

            logger.Log(StaticLocalizer[nameof(Resources.Program.Passkey_LoginSuccess)], user, TaskStatus.Success);
        }

        return Ok();
    }

    /// <summary>
    /// Get user's registered passkeys
    /// </summary>
    /// <remarks>
    /// Use this API to get list of user's registered passkeys. User permissions required.
    /// </remarks>
    /// <param name="token">Cancellation token</param>
    /// <response code="200">List of passkeys</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [RequireUser]
    [ProducesResponseType(typeof(PasskeyInfoModel[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Passkeys(CancellationToken token = default)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized(new RequestResponse(localizer[nameof(Resources.Program.Account_UserNotExist)],
                StatusCodes.Status401Unauthorized));

        var passkeys = await userManager.GetPasskeysAsync(user);
        var result = passkeys.Select(p => new PasskeyInfoModel
        {
            CredentialId = Convert.ToBase64String(p.CredentialId),
            Name = p.Name,
            CreatedAt = p.CreatedAt,
            IsBackedUp = p.IsBackedUp,
            Transports = p.Transports
        }).ToArray();

        return Ok(result);
    }

    /// <summary>
    /// Delete a passkey
    /// </summary>
    /// <remarks>
    /// Use this API to delete a passkey. User permissions required.
    /// </remarks>
    /// <param name="credentialId">Base64-encoded credential ID to delete</param>
    /// <param name="token">Cancellation token</param>
    /// <response code="200">Passkey deleted successfully</response>
    /// <response code="400">Invalid credential ID</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Passkey not found</response>
    [HttpDelete("{credentialId}")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePasskey(string credentialId, CancellationToken token = default)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized(new RequestResponse(localizer[nameof(Resources.Program.Account_UserNotExist)],
                StatusCodes.Status401Unauthorized));

        byte[] credentialIdBytes;
        try
        {
            credentialIdBytes = Convert.FromBase64String(credentialId);
        }
        catch (FormatException)
        {
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Passkey_InvalidCredentialId)]));
        }

        var passkey = await userManager.GetPasskeyAsync(user, credentialIdBytes);
        if (passkey is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Passkey_NotFound)],
                StatusCodes.Status404NotFound));

        var result = await userManager.RemovePasskeyAsync(user, credentialIdBytes);
        if (!result.Succeeded)
            return HandleIdentityError(result.Errors);

        logger.Log(StaticLocalizer[nameof(Resources.Program.Passkey_Deleted)], user, TaskStatus.Success);

        return Ok();
    }

    #endregion
}
