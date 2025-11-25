namespace GZCTF.Repositories.Interface;

public interface IOAuthProviderRepository : IRepository
{
    /// <summary>
    /// Fetches a provider configuration by its unique key (e.g., github, google).
    /// </summary>
    Task<OAuthProvider?> FindByKeyAsync(string key, CancellationToken token = default);

    /// <summary>
    /// Loads a provider record by its primary key identifier.
    /// </summary>
    Task<OAuthProvider?> FindByIdAsync(int id, CancellationToken token = default);

    /// <summary>
    /// Returns all configured OAuth providers for administrative scenarios.
    /// </summary>
    Task<List<OAuthProvider>> ListAsync(CancellationToken token = default);
}
