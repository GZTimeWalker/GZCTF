namespace GZCTF.Storage.Interface;

/// <summary>
/// Abstraction for blob/object storage backends used by the application.
/// </summary>
public interface IBlobStorage
{
    /// <summary>
    /// Ensures that the storage backend is properly initialized.
    /// </summary>
    Task EnsureInitializedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether an object exists at the specified path.
    /// </summary>
    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the object or directory located at the specified path.
    /// </summary>
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves metadata information for the object at the specified path.
    /// </summary>
    Task<StorageItem> GetBlobAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the number of objects directly under the specified path.
    /// </summary>
    Task<int> CountAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the object at the specified path for reading.
    /// </summary>
    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the provided stream to the specified path.
    /// </summary>
    Task WriteAsync(string path, Stream content, bool append = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies the specified local file into the storage backend at the provided path.
    /// </summary>
    Task WriteFileAsync(string path, string localFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes textual content to the specified path.
    /// </summary>
    Task WriteTextAsync(string path, string contents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads textual content from the specified path.
    /// </summary>
    Task<string> ReadTextAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists items under the specified path.
    /// </summary>
    Task<IReadOnlyList<StorageItem>> ListAsync(string path, bool useFlatListing = false,
        CancellationToken cancellationToken = default);
}
