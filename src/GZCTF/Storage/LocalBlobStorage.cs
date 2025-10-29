using System.Runtime.InteropServices;
using GZCTF.Storage.Interface;

namespace GZCTF.Storage;

/// <summary>
/// File system based blob storage implementation.
/// </summary>
public sealed class LocalBlobStorage : IBlobStorage
{
    readonly string _root;
    readonly string _rootWithSeparator;

    const int ReadBufferSize = 4096;
    const int WriteBufferSize = 8192;

    public LocalBlobStorage(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        _root = Path.GetFullPath(rootPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        _rootWithSeparator = _root + Path.DirectorySeparatorChar;
    }

    public Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_root))
            Directory.CreateDirectory(_root);

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePhysicalPath(path);
        return Task.FromResult(File.Exists(fullPath) || Directory.Exists(fullPath));
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePhysicalPath(path);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            return Task.CompletedTask;
        }

        if (!Directory.Exists(fullPath))
            return Task.CompletedTask;

        Directory.Delete(fullPath, true);
        return Task.CompletedTask;
    }

    public Task<StorageItem> GetBlobAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePhysicalPath(path);

        if (File.Exists(fullPath))
        {
            var info = new FileInfo(fullPath);
            return Task.FromResult(CreateFileItem(StoragePath.Normalize(path), info));
        }

        if (!Directory.Exists(fullPath))
            throw new FileNotFoundException($"Storage object '{path}' does not exist.");

        var name = Path.GetFileName(fullPath);
        var normalized = StoragePath.Normalize(path);
        return Task.FromResult(new StorageItem(normalized, name, true));
    }

    public Task<int> CountAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePhysicalPath(path);

        if (File.Exists(fullPath))
            return Task.FromResult(1);

        if (!Directory.Exists(fullPath))
            return Task.FromResult(0);

        var count = 0;
        foreach (var _ in Directory.EnumerateFileSystemEntries(fullPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            count++;
        }

        return Task.FromResult(count);
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePhysicalPath(path);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Storage object '{path}' does not exist.");

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            ReadBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);

        return Task.FromResult(stream);
    }

    public async Task WriteAsync(string path, Stream content, bool append = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var fullPath = ResolvePhysicalPath(path, ensureDirectory: true);
        var fileMode = append ? FileMode.Append : FileMode.Create;

        await using var fileStream = new FileStream(fullPath, fileMode, FileAccess.Write, FileShare.None,
            WriteBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await content.CopyToAsync(fileStream, cancellationToken);
    }

    public async Task WriteFileAsync(string path, string localFilePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localFilePath);

        var fullPath = ResolvePhysicalPath(path, ensureDirectory: true);

        const FileOptions copyOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;

        await using var source = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            WriteBufferSize, copyOptions);
        await using var destination = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None,
            WriteBufferSize, copyOptions);
        await source.CopyToAsync(destination, cancellationToken);
    }

    public async Task WriteTextAsync(string path, string contents, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePhysicalPath(path, ensureDirectory: true);
        await File.WriteAllTextAsync(fullPath, contents, cancellationToken);
    }

    public Task<string> ReadTextAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePhysicalPath(path);
        return File.ReadAllTextAsync(fullPath, cancellationToken);
    }

    public Task<IReadOnlyList<StorageItem>> ListAsync(string path, bool useFlatListing = false,
        CancellationToken cancellationToken = default)
    {
        var normalized = StoragePath.Normalize(path);
        var fullPath = ResolvePhysicalPath(normalized);

        if (File.Exists(fullPath))
        {
            var info = new FileInfo(fullPath);
            IReadOnlyList<StorageItem> single = [CreateFileItem(normalized, info)];
            return Task.FromResult(single);
        }

        if (!Directory.Exists(fullPath))
            return Task.FromResult<IReadOnlyList<StorageItem>>([]);

        List<StorageItem> results = [];

        foreach (var directory in Directory.EnumerateDirectories(fullPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = Path.GetFileName(directory);
            var itemPath = StoragePath.Combine(normalized, name);
            results.Add(new StorageItem(itemPath, name, true));
        }

        foreach (var file in Directory.EnumerateFiles(fullPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var info = new FileInfo(file);
            var itemPath = StoragePath.Combine(normalized, info.Name);
            results.Add(CreateFileItem(itemPath, info));
        }

        return Task.FromResult<IReadOnlyList<StorageItem>>(results);
    }

    static StorageItem CreateFileItem(string relativePath, FileInfo info) =>
        new(relativePath, info.Name, false, info.Length, info.LastWriteTimeUtc);

    string ResolvePhysicalPath(string path, bool ensureDirectory = false)
    {
        var normalized = StoragePath.Normalize(path);
        var segments = StoragePath.Split(normalized).ToArray();
        var combined = segments.Length == 0
            ? _root
            : Path.Combine(new[] { _root }.Concat(segments).ToArray());

        var physicalPath = Path.GetFullPath(combined);

        if (!IsSubPathOfRoot(physicalPath))
            throw new InvalidOperationException($"The path '{path}' is outside of the configured storage root.");

        if (!ensureDirectory || !StoragePath.TryGetDirectoryName(normalized, out var directory))
            return physicalPath;

        var dirPath = ResolvePhysicalPath(directory);
        Directory.CreateDirectory(dirPath);

        return physicalPath;
    }

    bool IsSubPathOfRoot(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return path.StartsWith(_rootWithSeparator, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(path, _root, StringComparison.OrdinalIgnoreCase);

        return path.StartsWith(_rootWithSeparator, StringComparison.Ordinal) ||
               string.Equals(path, _root, StringComparison.Ordinal);
    }
}
