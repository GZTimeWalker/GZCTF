using System.Net.Mime;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text.Json;
using GZCTF.Middlewares;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Response.Account;
using GZCTF.Repositories.Interface;
using GZCTF.Services;
using GZCTF.Services.Config;
using GZCTF.Services.Mail;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

using System.Text.RegularExpressions;
namespace GZCTF.Controllers;
public partial class AccountController
{
    [GeneratedRegex("^[a-f0-9]{64}$")]
    private static partial Regex BrowserFingerprintRegex();

    private sealed class BrowserFingerprintChallengeState
    {
        public DateTimeOffset IssuedAtUtc { get; set; }

        public string[] RequiredSignals { get; set; } = [];
    }
}

/// <summary>
/// User account related APIs
/// </summary>
[ApiController]
[Route("api/[controller]/[action]")]
[Produces(MediaTypeNames.Application.Json)]
public partial class AccountController(
    IMailSender mailSender,
    IBlobRepository blobService,
    IHostEnvironment environment,
    ICaptchaService captcha,
    IDistributedCache cache,
    IConfigService configService,
    IOptionsSnapshot<AccountPolicy> accountPolicy,
    IOptionsSnapshot<GlobalConfig> globalConfig,
    UserManager<UserInfo> userManager,
    SignInManager<UserInfo> signInManager,
    ILogger<AccountController> logger,
    IStringLocalizer<Program> localizer) : ControllerBase
{
    private static readonly string[] BrowserFingerprintProbeKeys =
    [
        "lie_count",
        "trash_count",
        "error_count",
        "headless_rating",
        "stealth_rating",
        "like_headless_rating",
        "platform_consistent",
        "ua_consistent",
        "webgl_consistent",
        "resistance_extension",
        "resistance_privacy"
    ];

    private static readonly DistributedCacheEntryOptions BrowserFingerprintChallengeCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
    };

    private const int BrowserFingerprintChallengeExpireSeconds = 120;

    /// <summary>
    /// Get browser fingerprint challenge
    /// </summary>
    /// <response code="200">Challenge generated successfully</response>
    [HttpGet]
    [ProducesResponseType(typeof(RequestResponse<BrowserFingerprintChallengeModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> FingerprintChallenge(CancellationToken token = default)
    {
        if (!accountPolicy.Value.EnableBrowserFingerprint)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Parameter_FingerprintInvalid)]));

        if (IsDisallowedFingerprintingBrowser(Request, out var challengeRejectionReason))
        {
            logger.LogWarning("Rejected fingerprint challenge for {RemoteIP}: {Reason}",
                HttpContext.Connection.RemoteIpAddress,
                challengeRejectionReason);
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Parameter_FingerprintInvalid)]));
        }

        var nonce = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(24));
        var requiredSignals = SelectRandomFingerprintProbeKeys();

        var state = new BrowserFingerprintChallengeState
        {
            IssuedAtUtc = DateTimeOffset.UtcNow,
            RequiredSignals = requiredSignals
        };

        await cache.SetStringAsync(BrowserFingerprintChallengeKey(nonce), JsonSerializer.Serialize(state),
            BrowserFingerprintChallengeCacheOptions, token);

        var data = new BrowserFingerprintChallengeModel
        {
            Nonce = nonce,
            RequiredSignals = requiredSignals,
            ExpiresInSeconds = BrowserFingerprintChallengeExpireSeconds
        };

        return Ok(new RequestResponse<BrowserFingerprintChallengeModel>(string.Empty, data, StatusCodes.Status200OK));
    }

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


        var user = new UserInfo 
        { 
            UserName = model.UserName, 
            Email = model.Email, 
            // Auto-assign Admin role in development for testing
            Role = environment.IsDevelopment() ? Role.Admin : Role.User 
        };

        user.UpdateByHttpContext(HttpContext);
        if (accountPolicy.Value.EnableBrowserFingerprint
            && (string.IsNullOrEmpty(model.Fingerprint) || string.IsNullOrEmpty(model.FingerprintProof)))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Parameter_FingerprintRequired)]));

        var fingerprint = await ValidateBrowserFingerprint(model.Fingerprint, model.FingerprintProof, token);
        if (accountPolicy.Value.EnableBrowserFingerprint && fingerprint is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Parameter_FingerprintInvalid)]));

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
            await signInManager.SignInWithClaimsAsync(user, true, BuildFingerprintClaims(fingerprint));

            logger.Log(StaticLocalizer[nameof(Resources.Program.Account_UserRegisteredLog)],
                user.UserName ?? "Anonymous",
                user.IP?.ToString(),
                TaskStatus.Success,
                fingerprint: fingerprint);
            return Ok(new RequestResponse<RegisterStatus>(localizer[nameof(Resources.Program.Account_UserRegistered)],
                RegisterStatus.LoggedIn,
                StatusCodes.Status200OK));
        }

        if (!accountPolicy.Value.EmailConfirmationRequired)
        {
            logger.Log(StaticLocalizer[nameof(Resources.Program.Account_UserRegisteredWaitingApprovalLog)],
                user.UserName ?? "Anonymous",
                user.IP?.ToString(),
                TaskStatus.Success,
                fingerprint: fingerprint);
            return Ok(new RequestResponse<RegisterStatus>(
                localizer[nameof(Resources.Program.Account_UserRegisteredWaitingApproval)],
                RegisterStatus.AdminConfirmationRequired, StatusCodes.Status200OK));
        }

        logger.Log(StaticLocalizer[nameof(Resources.Program.Account_SendEmailVerification)],
            user.UserName ?? "Anonymous",
            user.IP?.ToString(),
            TaskStatus.Pending,
            fingerprint: fingerprint);

        var rToken = Codec.Base64.Encode(await userManager.GenerateEmailConfirmationTokenAsync(user));
        var link = GetEmailLink("verify", rToken, model.Email);

        if (environment.IsDevelopment())
        {
            logger.Log(StaticLocalizer[nameof(Resources.Program.Account_SendEmailVerification)],
                user, TaskStatus.Pending, LogLevel.Debug);
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

    private bool VerifyEmailDomain(string email)
    {
        var mailDomain = email.Split('@')[1];

        return string.IsNullOrWhiteSpace(accountPolicy.Value.EmailDomainList)
               || accountPolicy.Value.EmailDomainList
                   .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                   .Any(d => d.Equals(mailDomain, StringComparison.InvariantCulture));
    }

    private async Task<string?> ValidateBrowserFingerprint(string? encryptedFingerprint, string? encryptedFingerprintProof,
        CancellationToken token = default)
    {
        if (!accountPolicy.Value.EnableBrowserFingerprint)
            return null;

        if (string.IsNullOrEmpty(encryptedFingerprint) || string.IsNullOrEmpty(encryptedFingerprintProof))
            return null;

        var fingerprint = configService.DecryptApiData(encryptedFingerprint);
        if (fingerprint is null || !BrowserFingerprintRegex().IsMatch(fingerprint))
            return null;

        var fingerprintProofRaw = configService.DecryptApiData(encryptedFingerprintProof);
        var fingerprintProof = BrowserFingerprintProofValidator.Parse(fingerprintProofRaw);

        if (fingerprintProof is null)
            return null;

        if (IsDisallowedFingerprintingBrowser(Request, out var browserRejectionReason))
        {
            logger.LogWarning("Rejected browser fingerprint proof from {RemoteIP}: {Reason}",
                HttpContext.Connection.RemoteIpAddress,
                browserRejectionReason);
            return null;
        }

        if (string.IsNullOrWhiteSpace(fingerprintProof.Nonce) || !IsValidFingerprintChallengeNonce(fingerprintProof.Nonce))
            return null;

        var challenge = await TryConsumeFingerprintChallenge(fingerprintProof.Nonce, token);
        if (challenge is null)
            return null;

        if (!BrowserFingerprintProofValidator.HasValidChallengeSignals(fingerprintProof, challenge.RequiredSignals,
                out var signalValidationReason))
        {
            logger.LogWarning("Rejected browser fingerprint proof from {RemoteIP}: {Reason}",
                HttpContext.Connection.RemoteIpAddress,
                signalValidationReason);
            return null;
        }

        if (!BrowserFingerprintProofValidator.IsTrusted(fingerprintProof, fingerprint, out var rejectionReason))
        {
            logger.LogWarning("Rejected browser fingerprint proof from {RemoteIP}: {Reason}",
                HttpContext.Connection.RemoteIpAddress,
                rejectionReason);
            return null;
        }

        return fingerprint;
    }

    private static bool IsValidFingerprintChallengeNonce(string nonce)
    {
        if (nonce.Length is < 16 or > 128)
            return false;

        return nonce.All(ch => ch is (>= 'a' and <= 'z')
            or (>= 'A' and <= 'Z')
            or (>= '0' and <= '9')
            or '-'
            or '_');
    }

    private static bool IsDisallowedFingerprintingBrowser(HttpRequest request, out string reason)
    {
        if (HeaderContains(request, "Sec-CH-UA", "Brave")
            || HeaderContains(request, "Sec-CH-UA-Full-Version-List", "Brave"))
        {
            reason = "Brave browser client hints detected";
            return true;
        }

        var userAgent = request.Headers.UserAgent.ToString();
        if (!string.IsNullOrWhiteSpace(userAgent) && userAgent.Contains("Brave", StringComparison.OrdinalIgnoreCase))
        {
            reason = "Brave browser user agent detected";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static bool HeaderContains(HttpRequest request, string headerName, string keyword)
    {
        if (!request.Headers.TryGetValue(headerName, out var values) || values.Count == 0)
            return false;

        foreach (var value in values)
        {
            if (!string.IsNullOrEmpty(value) && value.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private async Task<BrowserFingerprintChallengeState?> TryConsumeFingerprintChallenge(string nonce,
        CancellationToken token = default)
    {
        var key = BrowserFingerprintChallengeKey(nonce);
        var json = await cache.GetStringAsync(key, token);
        await cache.RemoveAsync(key, token);

        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var challenge = JsonSerializer.Deserialize<BrowserFingerprintChallengeState>(json);
            return challenge is { RequiredSignals.Length: > 0 } ? challenge : null;
        }
        catch
        {
            return null;
        }
    }

    private static string[] SelectRandomFingerprintProbeKeys()
    {
        var keys = BrowserFingerprintProbeKeys.ToArray();
        for (var i = keys.Length - 1; i > 0; i--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(i + 1);
            (keys[i], keys[swapIndex]) = (keys[swapIndex], keys[i]);
        }

        return keys;
    }

    private static string BrowserFingerprintChallengeKey(string nonce) =>
        $"_BrowserFingerprintChallenge_{nonce}";

    private static IEnumerable<Claim> BuildFingerprintClaims(string? fingerprint)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
            return [];

        return [new Claim(ContextHelper.BrowserFingerprintClaimType, fingerprint)];
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
            logger.Log(StaticLocalizer[nameof(Resources.Program.Account_SendEmailVerification)],
                user, TaskStatus.Pending, LogLevel.Debug);
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

        if (accountPolicy.Value.EnableBrowserFingerprint
            && (string.IsNullOrEmpty(model.Fingerprint) || string.IsNullOrEmpty(model.FingerprintProof)))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Parameter_FingerprintRequired)]));

        var fingerprint = await ValidateBrowserFingerprint(model.Fingerprint, model.FingerprintProof, token);
        if (accountPolicy.Value.EnableBrowserFingerprint && fingerprint is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Parameter_FingerprintInvalid)]));

        await signInManager.SignOutAsync();
        var result = await signInManager.CheckPasswordSignInAsync(user, password, false);

        if (!result.Succeeded)
            return Unauthorized(new RequestResponse(
                localizer[nameof(Resources.Program.Account_IncorrectUserNameOrPassword)],
                StatusCodes.Status401Unauthorized));

        user.LastSignedInUtc = DateTimeOffset.UtcNow;
        user.UpdateByHttpContext(HttpContext);
        await userManager.UpdateAsync(user);

        await signInManager.SignInWithClaimsAsync(user, true, BuildFingerprintClaims(fingerprint));

        logger.Log(StaticLocalizer[nameof(Resources.Program.Account_UserLogined)],
            user.UserName ?? "Anonymous",
            user.IP?.ToString(),
            TaskStatus.Success,
            fingerprint: fingerprint);

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
            logger.Log(StaticLocalizer[nameof(Resources.Program.Account_SendEmailChange)],
                user, TaskStatus.Pending, LogLevel.Debug);
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
    public async Task<IActionResult> Profile([FromServices] AppDbContext dbContext)
    {
        var user = await userManager.GetUserAsync(User);
        var model = ProfileUserInfoModel.FromUserInfo(user!);
        model.HasManagedGames = await dbContext.EventManagers.AnyAsync(e => e.UserId == user!.Id);

        return Ok(model);
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

    private string GetEmailLink(string action, string token, string? email)
        => $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/account/{action}?" +
           $"token={token}&email={Codec.Base64.Encode(email)}";

    private BadRequestObjectResult HandleIdentityError(IEnumerable<IdentityError> errors) =>
        BadRequest(new RequestResponse(errors.FirstOrDefault()?.Description ??
                                       localizer[nameof(Resources.Program.Identity_UnknownError)]));
}
