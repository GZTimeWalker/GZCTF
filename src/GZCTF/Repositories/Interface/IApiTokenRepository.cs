namespace GZCTF.Repositories.Interface;

/// <summary>
/// Repository for managing API tokens.
/// </summary>
public interface IApiTokenRepository
{
    /// <summary>
    /// Adds a new API token to the database.
    /// </summary>
    /// <param name="token">The token to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The added token.</returns>
    Task<ApiToken> AddTokenAsync(ApiToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all API tokens.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of all API tokens.</returns>
    Task<List<ApiToken>> GetTokensAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Record token usage by updating the LastUsedAt timestamp.
    /// </summary>
    /// <param name="id">The ID of the token.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The added token.</returns>
    Task<bool> RecordTokenUsageAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an API token by its ID.
    /// </summary>
    /// <param name="id">The ID of the token.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The API token, or null if not found.</returns>
    Task<ApiToken?> GetTokenByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes an API token.
    /// </summary>
    /// <param name="token">The token to revoke.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the token was revoked, false otherwise.</returns>
    Task<bool> RevokeTokenAsync(ApiToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a revoked API token.
    /// </summary>
    /// <param name="token">The token to revoke.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the token was restored, false otherwise.</returns>
    Task<bool> RestoreTokenAsync(ApiToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an API token.
    /// </summary>
    /// <param name="token">The token to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the token was deleted, false otherwise.</returns>
    Task<bool> DeleteTokenAsync(ApiToken token, CancellationToken cancellationToken = default);
}
