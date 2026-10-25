using System.Net.Mime;
using GZCTF.Middlewares;
using GZCTF.Models.Request.Account;
using GZCTF.Services.Config;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;

namespace GZCTF.Controllers;

/// <summary>
/// Passkey login and account management
/// </summary>
/// <remarks>
/// Mutating requests require the X-CSRF-TOKEN header and the matching cookie from GET /api/passkey/token.
/// Fetch a new token after signing in or changing accounts. The global rate limit applies to all endpoints.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[AutoValidateAntiforgeryToken]
[ApiAntiforgeryFailure]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status429TooManyRequests)]
public class PasskeyController(
    AppDbContext context,
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
    /// <response code="200">Request token returned and its matching cookie set</response>
    [HttpGet("token")]
    [ProducesResponseType<PasskeyAntiforgeryModel>(StatusCodes.Status200OK)]
    public IActionResult Token() => Ok(new PasskeyAntiforgeryModel
    {
        Token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken!
    });

    /// <summary>
    /// Start username-less passkey login
    /// </summary>
    /// <remarks>Creates a protected, short-lived ceremony cookie required by POST /api/passkey/login.</remarks>
    /// <response code="200">WebAuthn PublicKeyCredentialRequestOptions JSON</response>
    /// <response code="400">Request token validation failed</response>
    [HttpPost("options")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestOptions() =>
        Content(await signInManager.MakePasskeyRequestOptionsAsync(null), MediaTypeNames.Application.Json);

    /// <summary>
    /// Log in using a passkey
    /// </summary>
    /// <param name="model">Signed WebAuthn credential returned after requesting login options</param>
    /// <response code="200">Login successful</response>
    /// <response code="400">Request validation failed</response>
    /// <response code="401">Passkey verification failed, ceremony expired, or account cannot sign in</response>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
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
            return PasskeyFailure(StatusCodes.Status401Unauthorized);
        }

        if (!assertion.Succeeded)
            return PasskeyFailure(StatusCodes.Status401Unauthorized);

        var user = assertion.User;
        if (user.Role == Role.Banned || !await signInManager.CanSignInAsync(user) ||
            await userManager.IsLockedOutAsync(user))
            return PasskeyFailure(StatusCodes.Status401Unauthorized);

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
    /// <response code="200">Passkey metadata returned without public keys or attestation data</response>
    /// <response code="401">Login required</response>
    /// <response code="403">Account access forbidden</response>
    [HttpGet]
    [RequireUser]
    [ProducesResponseType<PasskeyModel[]>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
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
    /// <remarks>Creates a protected, short-lived ceremony cookie required by POST /api/passkey.</remarks>
    /// <param name="model">Current password confirmation</param>
    /// <response code="200">WebAuthn PublicKeyCredentialCreationOptions JSON</response>
    /// <response code="400">Invalid request, incorrect password, account locked, or passkey limit reached</response>
    /// <response code="401">Login required</response>
    /// <response code="403">Account access forbidden</response>
    [HttpPost("creation-options")]
    [RequireUser]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType<RequestResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreationOptions([FromBody] PasskeyCreationModel model)
    {
        var user = (await userManager.GetUserAsync(User))!;
        var password = configService.DecryptApiData(model.Password);
        if (string.IsNullOrWhiteSpace(password))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Model_PasswordRequired)]));

        var passwordResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (passwordResult.IsLockedOut)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Account_UserLockedOut)]));
        if (passwordResult.IsNotAllowed)
            return RequestResponse.Result(localizer[nameof(Resources.Program.Auth_AccessForbidden)],
                StatusCodes.Status403Forbidden);
        if (!passwordResult.Succeeded)
            return HandleIdentityError([userManager.ErrorDescriber.PasswordMismatch()]);

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
    /// <param name="model">WebAuthn registration response and passkey name</param>
    /// <response code="200">Passkey registered successfully</response>
    /// <response code="400">Request or attestation invalid, passkey limit reached, or account update failed</response>
    /// <response code="401">Login required</response>
    /// <response code="403">Account access forbidden</response>
    [HttpPost]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<RequestResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
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

        if (!attestation.Succeeded || attestation.UserEntity.Id != await userManager.GetUserIdAsync(user))
            return PasskeyFailure();

        await using var transaction = await context.Database.BeginTransactionAsync(HttpContext.RequestAborted);
        if ((await userManager.GetPasskeysAsync(user)).Count >= MaxPasskeys)
            return PasskeyLimit();

        attestation.Passkey.Name = model.Name.Trim();
        var result = await userManager.AddOrUpdatePasskeyAsync(user, attestation.Passkey);
        if (!result.Succeeded)
            return HandleIdentityError(result.Errors);

        await transaction.CommitAsync(HttpContext.RequestAborted);
        logger.Log(StaticLocalizer[nameof(Resources.Program.Account_PasskeyAdded)], user, TaskStatus.Success);
        return Ok();
    }

    /// <summary>
    /// Rename one of the current user's passkeys
    /// </summary>
    /// <param name="credentialId">Base64url-encoded credential ID</param>
    /// <param name="model">New passkey name</param>
    /// <response code="200">Passkey renamed successfully</response>
    /// <response code="400">Request validation or account update failed</response>
    /// <response code="401">Login required</response>
    /// <response code="403">Account access forbidden</response>
    /// <response code="404">Credential ID is invalid or does not belong to the current user</response>
    [HttpPut("{credentialId}")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<RequestResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rename(string credentialId, [FromBody] PasskeyRenameModel model)
    {
        var user = (await userManager.GetUserAsync(User))!;
        var passkey = await FindPasskey(user, credentialId);
        if (passkey is null)
            return NotFound();

        await using var transaction = await context.Database.BeginTransactionAsync(HttpContext.RequestAborted);
        passkey.Name = model.Name.Trim();
        var result = await userManager.AddOrUpdatePasskeyAsync(user, passkey);
        if (!result.Succeeded)
            return HandleIdentityError(result.Errors);

        await transaction.CommitAsync(HttpContext.RequestAborted);
        logger.Log(StaticLocalizer[nameof(Resources.Program.Account_PasskeyRenamed)], user, TaskStatus.Success);
        return Ok();
    }

    /// <summary>
    /// Remove one of the current user's passkeys
    /// </summary>
    /// <param name="credentialId">Base64url-encoded credential ID</param>
    /// <response code="200">Passkey removed successfully</response>
    /// <response code="400">Request validation or account update failed</response>
    /// <response code="401">Login required</response>
    /// <response code="403">Account access forbidden</response>
    /// <response code="404">Credential ID is invalid or does not belong to the current user</response>
    [HttpDelete("{credentialId}")]
    [RequireUser]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<RequestResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(string credentialId)
    {
        var user = (await userManager.GetUserAsync(User))!;
        var passkey = await FindPasskey(user, credentialId);
        if (passkey is null)
            return NotFound();

        await using var transaction = await context.Database.BeginTransactionAsync(HttpContext.RequestAborted);
        var result = await userManager.RemovePasskeyAsync(user, passkey.CredentialId);
        if (!result.Succeeded)
            return HandleIdentityError(result.Errors);

        await transaction.CommitAsync(HttpContext.RequestAborted);
        logger.Log(StaticLocalizer[nameof(Resources.Program.Account_PasskeyRemoved)], user, TaskStatus.Success);
        return Ok();
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

    private BadRequestObjectResult HandleIdentityError(IEnumerable<IdentityError> errors) =>
        BadRequest(new RequestResponse(errors.FirstOrDefault()?.Description ??
                                       localizer[nameof(Resources.Program.Identity_UnknownError)]));
}
