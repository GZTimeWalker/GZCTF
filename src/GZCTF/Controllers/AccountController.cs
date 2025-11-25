using System.Net.Mime;
using GZCTF.Extensions.Startup;
using GZCTF.Middlewares;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Account;
using GZCTF.Repositories.Interface;
using GZCTF.Services;
using GZCTF.Services.Cache;
using GZCTF.Services.Config;
using GZCTF.Services.Mail;
using GZCTF.Services.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.WebUtilities;
using UserMetadataField = GZCTF.Models.Internal.UserMetadataField;

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
    IOAuthProviderManager oauthManager,
    IOAuthService oauthService,
    CacheHelper cacheHelper,
    IUserMetadataService userMetadataService,
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

        var metadataValidation = await userMetadataService.ValidateAsync(
            model.Metadata,
            null,
            allowLockedWrites: false,
            enforceLockedRequirements: false,
            token);

        if (!metadataValidation.IsValid)
            return BadRequest(new RequestResponse(metadataValidation.Errors.First()));

        var user = new UserInfo
        {
            UserName = model.UserName,
            Email = model.Email,
            Role = Role.User,
            UserMetadata = metadataValidation.Values
        };

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
    /// <param name="token"></param>
    /// <response code="200">User data updated successfully</response>
    /// <response code="400">Validation failed or user data update failed</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromBody] ProfileUpdateModel model, CancellationToken token = default)
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

        if (model.Metadata is not null)
        {
            var metadataResult = await userMetadataService.ValidateAsync(
                model.Metadata,
                user!.UserMetadata,
                allowLockedWrites: false,
                enforceLockedRequirements: true,
                token);

            if (!metadataResult.IsValid)
                return BadRequest(new RequestResponse(metadataResult.Errors.First()));

            user.UserMetadata = metadataResult.Values;
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
    /// Update user metadata
    /// </summary>
    /// <remarks>
    /// Allows user to edit unlocked metadata fields.
    /// </remarks>
    /// <response code="200">Metadata updated successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut("/api/Account/Metadata")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMetadata(
        [FromBody] UserMetadataUpdateModel model,
        CancellationToken token = default)
    {
        var user = await userManager.GetUserAsync(User);

        var validation = await userMetadataService.ValidateAsync(
            model.Metadata,
            user!.UserMetadata,
            allowLockedWrites: false,
            enforceLockedRequirements: true,
            token);

        if (!validation.IsValid)
            return BadRequest(new RequestResponse(validation.Errors.First()));

        user.UserMetadata = validation.Values;
        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return HandleIdentityError(result.Errors);

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

    /// <summary>
    /// Get user metadata field configuration
    /// </summary>
    /// <remarks>
    /// Use this API to get configured user metadata fields.
    /// </remarks>
    /// <response code="200">User metadata fields configuration retrieved successfully</response>
    [HttpGet("/api/Account/MetadataFields")]
    [ProducesResponseType(typeof(List<UserMetadataField>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MetadataFields(CancellationToken token = default)
    {
        var fields = await oauthManager.GetUserMetadataFieldsAsync(token);
        return Ok(fields);
    }

    /// <summary>
    /// Get available OAuth providers
    /// </summary>
    /// <remarks>
    /// Use this API to get available OAuth providers for login.
    /// </remarks>
    /// <response code="200">Available OAuth providers</response>
    [HttpGet("/api/Account/OAuth/Providers")]
    [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOAuthProviders(CancellationToken token = default)
    {
        var providers = await oauthManager.GetOAuthProvidersAsync(token);
        var availableProviders = providers
            .Where(p => p.Value.Enabled)
            .ToDictionary(p => p.Key, p => p.Value.DisplayName ?? p.Key);

        return Ok(availableProviders);
    }

    /// <summary>
    /// Initiate OAuth login
    /// </summary>
    /// <remarks>
    /// Use this API to initiate OAuth login with a provider. Returns the authorization URL.
    /// </remarks>
    /// <param name="provider">Provider key (e.g., google, github)</param>
    /// <param name="token">Cancellation token</param>
    /// <response code="200">Authorization URL returned</response>
    /// <response code="400">Invalid provider or provider not enabled</response>
    [HttpGet("/api/Account/OAuth/Login/{provider}")]
    [ProducesResponseType(typeof(RequestResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OAuthLogin(
        string provider,
        CancellationToken token = default)
    {
        var providerConfig = await oauthManager.GetOAuthProviderAsync(provider, token);

        if (providerConfig is null || !providerConfig.Enabled)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_UserNotExist)]));

        // Generate state for CSRF protection
        var state = Guid.NewGuid().ToString("N");
        var cacheKey = CacheKey.OAuthState(state);

        // Store state in cache for validation (10 minutes expiry)
        await cacheHelper.SetStringAsync(
            cacheKey,
            provider,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) },
            token);

        var redirectUri = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/api/Account/OAuth/Callback/{provider}";
        var queryParameters = new Dictionary<string, string?>
        {
            ["client_id"] = providerConfig.ClientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["state"] = state
        };

        var scopes = providerConfig.Scopes?.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        if (scopes is { Length: > 0 })
            queryParameters["scope"] = string.Join(" ", scopes);

        var authUrl = QueryHelpers.AddQueryString(providerConfig.AuthorizationEndpoint, queryParameters);

        return Ok(new RequestResponse<string>(
            "OAuth authorization URL",
            authUrl,
            StatusCodes.Status200OK));
    }

    /// <summary>
    /// OAuth callback endpoint
    /// </summary>
    /// <remarks>
    /// This endpoint handles OAuth callbacks from providers. Do not call directly.
    /// </remarks>
    /// <param name="provider">Provider key</param>
    /// <param name="code">Authorization code</param>
    /// <param name="state">State parameter</param>
    /// <param name="error">Error returned by provider</param>
    /// <param name="token">Cancellation token</param>
    /// <response code="302">Redirects to frontend with result</response>
    [HttpGet("/api/Account/OAuth/Callback/{provider}")]
    public async Task<IActionResult> OAuthCallback(
        string provider,
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            logger.SystemLog(
                $"OAuth callback missing state for provider {provider}",
                TaskStatus.Failed,
                LogLevel.Warning);

            return Redirect("/account/login?error=oauth_state_missing");
        }

        // Validate state
        var cacheKey = CacheKey.OAuthState(state);
        var storedProvider = await cacheHelper.GetStringAsync(cacheKey, token);
        if (string.IsNullOrEmpty(storedProvider) || storedProvider != provider)
        {
            logger.SystemLog(
                $"OAuth callback state mismatch for provider {provider}",
                TaskStatus.Failed,
                LogLevel.Warning);

            return Redirect("/account/login?error=oauth_state_mismatch");
        }

        // Clear state
        await cacheHelper.RemoveAsync(cacheKey, token);

        if (!string.IsNullOrEmpty(error))
        {
            logger.SystemLog(
                $"OAuth error from provider {provider}: {error}",
                TaskStatus.Failed,
                LogLevel.Warning);

            return Redirect("/account/login?error=oauth_error");
        }

        if (string.IsNullOrEmpty(code))
            return Redirect("/account/login?error=oauth_no_code");

        try
        {
            // Exchange code for user info
            var redirectUri = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/api/Account/OAuth/Callback/{provider}";
            var oauthUser = await oauthService.ExchangeCodeForUserInfoAsync(provider, code, redirectUri, token);

            if (oauthUser is null)
            {
                logger.SystemLog(
                    $"Failed to exchange OAuth code for provider {provider}",
                    TaskStatus.Failed,
                    LogLevel.Warning);

                return Redirect("/account/login?error=oauth_exchange_failed");
            }

            // Get or create user
            var (user, isNewUser) = await oauthService.GetOrCreateUserFromOAuthAsync(provider, oauthUser, token);

            // Sign in the user
            await signInManager.SignInAsync(user, isPersistent: true);

            logger.SystemLog(
                $"User {user.Email} {(isNewUser ? "registered and" : "")} logged in via OAuth provider {provider}",
                TaskStatus.Success,
                LogLevel.Information);

            // Redirect to appropriate page
            return Redirect(isNewUser ? "/account/profile?firstLogin=true" : "/");
        }
        catch (OAuthLoginException ex)
        {
            logger.LogWarning(ex, "OAuth login failed for provider {Provider}", provider);
            return Redirect($"/account/login?error={ex.ErrorCode}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing OAuth callback for provider {Provider}", provider);
            return Redirect($"/account/login?error=oauth_processing_error");
        }
    }

    string GetEmailLink(string action, string token, string? email)
        => $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/account/{action}?" +
           $"token={token}&email={Codec.Base64.Encode(email)}";

    BadRequestObjectResult HandleIdentityError(IEnumerable<IdentityError> errors) =>
        BadRequest(new RequestResponse(errors.FirstOrDefault()?.Description ??
                                       localizer[nameof(Resources.Program.Identity_UnknownError)]));
}
