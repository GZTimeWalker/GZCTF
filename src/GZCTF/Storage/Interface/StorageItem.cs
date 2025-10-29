namespace GZCTF.Storage.Interface;

/// <summary>
/// Represents an item stored in the configured blob storage backend.
/// </summary>
public sealed class StorageItem(
    string fullPath,
    string name,
    bool isDirectory,
    long? size = null,
    DateTimeOffset? lastModificationTime = null)
{
    /// <summary>
    /// Gets the normalized full path of the object within the storage backend.
    /// </summary>
    public string FullPath { get; } = StoragePath.Normalize(fullPath);

    /// <summary>
    /// Gets the final segment of the path.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets a value indicating whether the item represents a directory.
    /// </summary>
    public bool IsDirectory { get; } = isDirectory;

    /// <summary>
    /// Gets the size of the item when it is a file.
    /// </summary>
    public long? Size { get; } = size;

    /// <summary>
    /// Gets the last modification time of the item when it is a file.
    /// </summary>
    public DateTimeOffset? LastModificationTime { get; } = lastModificationTime;
}
