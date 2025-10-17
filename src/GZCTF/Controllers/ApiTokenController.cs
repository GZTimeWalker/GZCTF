using System.Net.Mime;
using GZCTF.Middlewares;
using GZCTF.Models.Request.Admin;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Token;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Controllers;

/// <summary>
/// Controller for managing API tokens.
/// </summary>
[RequireAdmin]
[ApiController]
[Route("api/tokens")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class ApiTokenController(
    UserManager<UserInfo> userManager,
    IApiTokenRepository apiTokenRepository,
    ITokenService tokenService) : ControllerBase
{
    /// <summary>
    /// Generates a new API token.
    /// </summary>
    /// <param name="model">The model for creating the token</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generated token.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiTokenResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateToken([FromBody] ApiTokenCreateModel model,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.GetUserAsync(User);

        var apiToken = new ApiToken
        {
            Name = model.Name,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatorId = user?.Id,
            Creator = user
        };

        if (model.ExpiresIn is > 0)
            apiToken.ExpiresAt = apiToken.CreatedAt.AddDays(model.ExpiresIn.Value);

        await apiTokenRepository.AddTokenAsync(apiToken, cancellationToken);
        var token = await tokenService.GenerateToken(apiToken, cancellationToken);

        return Ok(new ApiTokenResponse(token, apiToken));
    }

    /// <summary>
    /// Lists all API tokens.
    /// </summary>
    /// <returns>A list of all API tokens.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ApiToken>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTokens(CancellationToken cancellationToken = default)
    {
        var tokens = await apiTokenRepository.GetTokensAsync(cancellationToken);
        return Ok(tokens);
    }

    /// <summary>
    /// Restores an API token.
    /// </summary>
    /// <param name="id">The ID of the token to restore.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreToken(Guid id, CancellationToken cancellationToken = default)
    {
        var token = await apiTokenRepository.GetTokenByIdAsync(id, cancellationToken);
        if (token == null)
            return NotFound();

        await apiTokenRepository.RestoreTokenAsync(token, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Revokes an API token.
    /// </summary>
    /// <param name="id">The ID of the token to revoke.</param>
    /// <param name="delete">If true, the token will be deleted instead of just revoked.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A success message if the token was revoked.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeToken(Guid id, [FromQuery] bool delete = false,
        CancellationToken cancellationToken = default)
    {
        var token = await apiTokenRepository.GetTokenByIdAsync(id, cancellationToken);
        if (token == null)
            return NotFound();

        if (delete)
            await apiTokenRepository.DeleteTokenAsync(token, cancellationToken);
        else
            await apiTokenRepository.RevokeTokenAsync(token, cancellationToken);

        return Ok();
    }
}
