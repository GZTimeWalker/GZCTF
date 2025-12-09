namespace GZCTF.Repositories.Interface;

public interface IUserMetadataFieldRepository : IRepository
{
    /// <summary>
    /// Get a user metadata field by key
    /// </summary>
    /// <param name="key">Field key</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>The field or null if not found</returns>
    Task<UserMetadataField?> GetByKeyAsync(string key, CancellationToken token = default);

    /// <summary>
    /// Get all user metadata fields
    /// </summary>
    /// <param name="token">Cancellation token</param>
    /// <returns>Array of fields</returns>
    Task<UserMetadataField[]> GetAllAsync(CancellationToken token = default);

    /// <summary>
    /// Create a new user metadata field
    /// </summary>
    /// <param name="field">Field to create</param>
    /// <param name="token">Cancellation token</param>
    Task CreateAsync(UserMetadataField field, CancellationToken token = default);

    /// <summary>
    /// Update an existing user metadata field
    /// </summary>
    /// <param name="field">Field to update</param>
    /// <param name="token">Cancellation token</param>
    Task UpdateAsync(UserMetadataField field, CancellationToken token = default);

    /// <summary>
    /// Delete a user metadata field by key
    /// </summary>
    /// <param name="key">Field key</param>
    /// <param name="token">Cancellation token</param>
    Task DeleteAsync(string key, CancellationToken token = default);
}
