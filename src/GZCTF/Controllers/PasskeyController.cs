using System.Net.Mime;
using GZCTF.Middlewares;
using GZCTF.Models.Request.Account;
using GZCTF.Services.Config;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;

namespace GZCTF.Controllers;

/// <summary>
/// Passkey login and account management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PasskeyController(
    UserManager<UserInfo> userManager,
    SignInManager<UserInfo> signInManager,
    IAntiforgery antiforgery,
    IConfigService configService,
    ILogger<PasskeyController> logger,
    IStringLocalizer<Program> localizer) : ControllerBase
{
    private const int MaxPasskeys = 10;

    /// <summary>
    /// Get a CSRF token for passkey operations, including anonymous login
    /// </summary>
    [HttpGet("token")]
    [ProducesResponseType<PasskeyAntiforgeryModel>(StatusCodes.Status200OK)]
    public IActionResult Token() => Ok(new PasskeyAntiforgeryModel
    {
        Token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken!
    });

    /// <summary>
    /// Start username-less passkey login
    /// </summary>
    [HttpPost("options")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Register))]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestOptions() =>
        Content(await signInManager.MakePasskeyRequestOptionsAsync(null), MediaTypeNames.Application.Json);

    /// <summary>
    /// Log in using a passkey
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Register))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<RequestResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogIn([FromBody] PasskeyCredentialModel model)
    {
        PasskeyAssertionResult<UserInfo> assertion;
        try
        {
            assertion = await signInManager.PerformPasskeyAssertionAsync(model.CredentialJson);
        }
        catch (InvalidOperationException)
        {
            // Missing, expired, or replaced ceremony state is a retryable client error.
            return PasskeyFailure(StatusCodes.Status401Unauthorized);
        }

        if (!assertion.Succeeded)
            return PasskeyFailure(StatusCodes.Status401Unauthorized);

        var user = assertion.User;
        if (user.Role == Role.Banned || !await signInManager.CanSignInAsync(user) ||
            await userManager.IsLockedOutAsync(user))
            return PasskeyFailure(StatusCodes.Status401Unauthorized);

        // Persist the verified signature counter and flags before issuing a session.
        user.LastSignedInUtc = DateTimeOffset.UtcNow;
        user.UpdateByHttpContext(HttpContext);
        var result = await userManager.AddOrUpdatePasskeyAsync(user, assertion.Passkey);
        if (!result.Succeeded)
            return PasskeyFailure(StatusCodes.Status401Unauthorized);

        await signInManager.SignInAsync(user, isPersistent: true, authenticationMethod: "passkey");
        logger.Log(StaticLocalizer[nameof(Resources.Program.Account_UserLogined)], user, TaskStatus.Success);
        return Ok();
    }

    /// <summary>
    /// List the current user's passkeys
    /// </summary>
    [HttpGet]
    [RequireUser]
    [ProducesResponseType<PasskeyModel[]>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var user = (await userManager.GetUserAsync(User))!;
        var passkeys = await userManager.GetPasskeysAsync(user);
        return Ok(passkeys.OrderBy(p => p.CreatedAt).Select(p => new PasskeyModel
        {
            CredentialId = WebEncoders.Base64UrlEncode(p.CredentialId),
            Name = p.Name,
            CreatedAt = p.CreatedAt
        }).ToArray());
    }

    /// <summary>
    /// Confirm the current password and start registering a passkey
    /// </summary>
    [HttpPost("creation-options")]
    [RequireUser]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Register))]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType<RequestResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreationOptions([FromBody] PasskeyCreationModel model)
    {
        var user = (await userManager.GetUserAsync(User))!;
        var password = configService.DecryptApiData(model.Password);
        if (string.IsNullOrWhiteSpace(password) ||
            !(await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)).Succeeded)
            return PasskeyFailure();

        if ((await userManager.GetPasskeysAsync(user)).Count >= MaxPasskeys)
            return PasskeyLimit();

        var options = await signInManager.MakePasskeyCreationOptionsAsync(new PasskeyUserEntity
        {
            Id = await userManager.GetUserIdAsync(user),
            Name = user.UserName!,
            DisplayName = user.UserName!
        });
        return Content(options, MediaTypeNames.Application.Json);
    }

    /// <summary>
    /// Verify and save a passkey for the current user
    /// </summary>
    [HttpPost]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<RequestResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] PasskeyRegistrationModel model)
    {
        var user = (await userManager.GetUserAsync(User))!;
        PasskeyAttestationResult attestation;
        try
        {
            attestation = await signInManager.PerformPasskeyAttestationAsync(model.CredentialJson);
        }
        catch (InvalidOperationException)
        {
            return PasskeyFailure();
        }

        // A browser may switch accounts while a registration prompt is open.
        if (!attestation.Succeeded || attestation.UserEntity.Id != await userManager.GetUserIdAsync(user))
            return PasskeyFailure();

        if ((await userManager.GetPasskeysAsync(user)).Count >= MaxPasskeys)
            return PasskeyLimit();

        attestation.Passkey.Name = model.Name.Trim();
        var result = await userManager.AddOrUpdatePasskeyAsync(user, attestation.Passkey);
        return result.Succeeded ? Ok() : PasskeyFailure();
    }

    /// <summary>
    /// Rename one of the current user's passkeys
    /// </summary>
    [HttpPut("{credentialId}")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<RequestResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rename(string credentialId, [FromBody] PasskeyRenameModel model)
    {
        var user = (await userManager.GetUserAsync(User))!;
        var passkey = await FindPasskey(user, credentialId);
        if (passkey is null)
            return NotFound();

        passkey.Name = model.Name.Trim();
        var result = await userManager.AddOrUpdatePasskeyAsync(user, passkey);
        return result.Succeeded ? Ok() : PasskeyFailure();
    }

    /// <summary>
    /// Remove one of the current user's passkeys
    /// </summary>
    [HttpDelete("{credentialId}")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<RequestResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(string credentialId)
    {
        var user = (await userManager.GetUserAsync(User))!;
        var passkey = await FindPasskey(user, credentialId);
        if (passkey is null)
            return NotFound();

        var result = await userManager.RemovePasskeyAsync(user, passkey.CredentialId);
        return result.Succeeded ? Ok() : PasskeyFailure();
    }

    private async Task<UserPasskeyInfo?> FindPasskey(UserInfo user, string credentialId)
    {
        try
        {
            return await userManager.GetPasskeyAsync(user, WebEncoders.Base64UrlDecode(credentialId));
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private ObjectResult PasskeyFailure(int status = StatusCodes.Status400BadRequest) =>
        StatusCode(status, new RequestResponse(localizer[nameof(Resources.Program.Account_PasskeyFailed)], status));

    private BadRequestObjectResult PasskeyLimit() =>
        BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_PasskeyLimit), MaxPasskeys]));
}
