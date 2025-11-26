using GZCTF.Models.Internal;
using InternalUserMetadataField = GZCTF.Models.Internal.UserMetadataField;

namespace GZCTF.Repositories.Interface;

/// <summary>
/// Provides data-access helpers for OAuth providers and user metadata field definitions stored in the database.
/// </summary>
public interface IOAuthProviderRepository : IRepository
{
    /// <summary>
    /// Finds an OAuth provider entity by its unique key, or returns <c>null</c> when it is missing.
    /// </summary>
    /// <param name="key">Provider key enforced by <see cref="OAuthProvider.ValidateKey(string)"/>.</param>
    /// <param name="token">Cancellation token for the underlying EF Core query.</param>
    /// <returns>The matching provider entity or <c>null</c>.</returns>
    Task<OAuthProvider?> FindByKeyAsync(string key, CancellationToken token = default);

    /// <summary>
    /// Retrieves all OAuth providers without tracking for display or administrative operations.
    /// </summary>
    /// <param name="token">Cancellation token for the query.</param>
    /// <returns>List of provider entities ordered as stored.</returns>
    Task<List<OAuthProvider>> ListAsync(CancellationToken token = default);

    /// <summary>
    /// Returns an immutable map of provider keys to their configuration snapshots.
    /// </summary>
    /// <param name="token">Cancellation token for the query.</param>
    /// <returns>Dictionary keyed by provider identifier with serialized configs.</returns>
    Task<Dictionary<string, OAuthProviderConfig>> GetConfigMapAsync(CancellationToken token = default);

    /// <summary>
    /// Loads a single provider configuration by key, enabling controllers to hydrate DTOs without EF entities.
    /// </summary>
    /// <param name="key">Provider identifier to lookup.</param>
    /// <param name="token">Cancellation token for the query.</param>
    /// <returns>The provider configuration or <c>null</c>.</returns>
    Task<OAuthProviderConfig?> GetConfigAsync(string key, CancellationToken token = default);

    /// <summary>
    /// Creates or updates a provider definition with the supplied configuration payload.
    /// </summary>
    /// <param name="key">Provider identifier to insert or update.</param>
    /// <param name="config">Configuration payload sent from the admin UI.</param>
    /// <param name="token">Cancellation token for the write operation.</param>
    Task UpsertAsync(string key, OAuthProviderConfig config, CancellationToken token = default);

    /// <summary>
    /// Deletes the provider identified by the given key when it exists.
    /// </summary>
    /// <param name="key">Provider identifier to remove.</param>
    /// <param name="token">Cancellation token for the delete operation.</param>
    Task DeleteAsync(string key, CancellationToken token = default);

    /// <summary>
    /// Fetches the ordered list of user metadata field definitions consumed by OAuth registration flows.
    /// </summary>
    /// <param name="token">Cancellation token for the query.</param>
    /// <returns>List of metadata descriptors sorted by their configured order.</returns>
    Task<List<InternalUserMetadataField>> GetMetadataFieldsAsync(CancellationToken token = default);

    /// <summary>
    /// Replaces the persisted metadata field definitions, preserving the provided ordering.
    /// </summary>
    /// <param name="fields">New metadata field collection in desired order.</param>
    /// <param name="token">Cancellation token for the write operation.</param>
    Task UpdateMetadataFieldsAsync(List<InternalUserMetadataField> fields, CancellationToken token = default);
}
