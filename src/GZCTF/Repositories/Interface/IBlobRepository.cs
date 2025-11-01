namespace GZCTF.Repositories.Interface;

public interface IBlobRepository : IRepository
{
    /// <summary>
    /// Create or update a blob file
    /// </summary>
    /// <param name="file">The form file</param>
    /// <param name="fileName">The name to save the file as</param>
    /// <param name="token"></param>
    /// <returns>The file object</returns>
    public Task<LocalFile> CreateOrUpdateBlob(IFormFile file, string? fileName = null,
        CancellationToken token = default);

    /// <summary>
    /// Create or update a blob file from a stream
    /// </summary>
    /// <param name="fileName">The name to save the file as</param>
    /// <param name="stream">The file content stream</param>
    /// <param name="token"></param>
    /// <returns>The file object</returns>
    public Task<LocalFile> CreateOrUpdateBlobFromStream(string fileName, Stream stream,
        CancellationToken token = default);

    /// <summary>
    /// Increment reference count for an existing blob file
    /// </summary>
    /// <param name="fileHash">The file hash</param>
    /// <param name="token"></param>
    /// <returns>The updated file object, or null if file not found</returns>
    public Task<LocalFile?> IncrementBlobReference(string fileHash, CancellationToken token = default);

    /// <summary>
    /// Create or update an image file, optionally resizing it
    /// </summary>
    /// <param name="file">The form file</param>
    /// <param name="fileName">The name to save the file as</param>
    /// <param name="resize">Resize dimension, set to 0 to skip resizing</param>
    /// <param name="token"></param>
    /// <returns>The file object</returns>
    public Task<LocalFile?> CreateOrUpdateImage(IFormFile file, string fileName, int resize = 300,
        CancellationToken token = default);

    /// <summary>
    /// Delete a blob file
    /// </summary>
    /// <param name="file">The file object</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<TaskStatus> DeleteBlob(LocalFile file, CancellationToken token = default);

    /// <summary>
    /// Delete a blob file by its hash
    /// </summary>
    /// <param name="fileHash">File hash</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<TaskStatus> DeleteBlobByHash(string fileHash, CancellationToken token = default);

    /// <summary>
    /// Get a blob file by its hash
    /// </summary>
    /// <param name="fileHash">The file hash</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<LocalFile?> GetBlobByHash(string? fileHash, CancellationToken token = default);

    /// <summary>
    /// Get all blob files with pagination
    /// </summary>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<LocalFile[]> GetBlobs(int count, int skip, CancellationToken token = default);

    /// <summary>
    /// Delete an attachment and its associated blob file
    /// </summary>
    /// <param name="attachment"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task DeleteAttachment(Attachment? attachment, CancellationToken token = default);
}
