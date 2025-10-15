namespace GZCTF.Services.Token;

/// <summary>
/// Service for handling token generation and validation.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates the API token.
    /// </summary>
    /// <param name="apiToken">The API token meta.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A token string.</returns>
    Task<string> GenerateToken(ApiToken apiToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the given token and returns the API token if valid.
    /// </summary>
    /// <param name="token">The token to validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>true if the token is valid, false otherwise.</returns>
    Task<bool> ValidateToken(string token, CancellationToken cancellationToken = default);
}
