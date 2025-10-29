using System.Net;
using System.Runtime.CompilerServices;
using Amazon.S3;
using Amazon.S3.Model;
using GZCTF.Storage.Interface;

namespace GZCTF.Storage;

/// <summary>
/// Amazon S3 compatible blob storage implementation.
/// </summary>
public sealed class S3BlobStorage : IBlobStorage, IDisposable
{
    readonly IAmazonS3 _client;
    readonly string _bucket;

    public S3BlobStorage(IAmazonS3 client, string bucket)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket);

        _client = client;
        _bucket = bucket;
    }

    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HeadBucketRequest { BucketName = _bucket };
            await _client.HeadBucketAsync(request, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            var request = new PutBucketRequest { BucketName = _bucket };
            await _client.PutBucketAsync(request, cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        var key = NormalizeKey(path);
        if (string.IsNullOrEmpty(key))
            return true;
        if (await TryGetObjectMetadataAsync(key, cancellationToken) is not null)
            return true;

        var prefix = EnsureDirectoryPrefix(key);
        var request = new ListObjectsV2Request { BucketName = _bucket, Prefix = prefix, MaxKeys = 1 };

        var response = await _client.ListObjectsV2Async(request, cancellationToken);
        return response.KeyCount > 0;
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var key = NormalizeKey(path);
        if (string.IsNullOrEmpty(key))
            return;

        List<KeyVersion> batch = [];

        var meta = await TryGetObjectMetadataAsync(key, cancellationToken);
        if (meta is not null)
            batch.Add(new KeyVersion { Key = key });

        await foreach (var childKey in EnumerateKeysAsync(EnsureDirectoryPrefix(key), cancellationToken))
        {
            batch.Add(new KeyVersion { Key = childKey });
            if (batch.Count >= 1000)
                await FlushAsync();
        }

        await FlushAsync();
        return;

        async Task FlushAsync()
        {
            if (batch.Count == 0)
                return;

            var deleteRequest = new DeleteObjectsRequest { BucketName = _bucket, Objects = batch, Quiet = true };

            batch = [];
            try
            {
                await _client.DeleteObjectsAsync(deleteRequest, cancellationToken);
            }
            catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                // ignore missing keys
            }
        }
    }

    public async Task<StorageItem> GetBlobAsync(string path, CancellationToken cancellationToken = default)
    {
        var key = NormalizeKey(path);
        if (string.IsNullOrEmpty(key))
            return new StorageItem(string.Empty, string.Empty, true);

        var meta = await TryGetObjectMetadataAsync(key, cancellationToken);
        if (meta is not null)
            return new StorageItem(key, GetLeafName(key), false, meta.ContentLength, meta.LastModified);

        var prefix = EnsureDirectoryPrefix(key);
        var request = new ListObjectsV2Request { BucketName = _bucket, Prefix = prefix, MaxKeys = 1 };

        var response = await _client.ListObjectsV2Async(request, cancellationToken);
        if (response.KeyCount > 0)
            return new StorageItem(key, GetLeafName(key), true);

        throw new FileNotFoundException($"Storage object '{path}' does not exist.");
    }

    public async Task<int> CountAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeKey(path);

        if (!string.IsNullOrEmpty(normalized))
        {
            var meta = await TryGetObjectMetadataAsync(normalized, cancellationToken);
            if (meta is not null)
                return 1;
        }

        var prefix = EnsureDirectoryPrefix(normalized);
        var request = new ListObjectsV2Request { BucketName = _bucket, Prefix = prefix, Delimiter = "/" };

        var total = 0;
        do
        {
            var response = await _client.ListObjectsV2Async(request, cancellationToken);

            if (response.CommonPrefixes is { } prefixes)
                total += prefixes.Count;

            if (response.S3Objects is { } objects)
            {
                total += objects.Where(obj =>
                        string.IsNullOrEmpty(prefix) || !string.Equals(obj.Key, prefix, StringComparison.Ordinal))
                    .Count(obj => !obj.Key.EndsWith('/'));
            }

            request.ContinuationToken = response.IsTruncated is true ? response.NextContinuationToken : null;
        } while (!string.IsNullOrEmpty(request.ContinuationToken));

        return total;
    }

    public async Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        var key = NormalizeKey(path);
        if (string.IsNullOrEmpty(key))
            throw new InvalidOperationException("Cannot open the storage root for reading.");

        try
        {
            var response = await _client.GetObjectAsync(_bucket, key, cancellationToken);
            if (response.ResponseStream is not null)
                return new S3ObjectReadStream(response);

            response.Dispose();
            throw new InvalidOperationException($"Object '{key}' returned no data stream.");
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException($"Storage object '{path}' does not exist.", ex);
        }
    }

    public async Task WriteAsync(string path, Stream content, bool append = false,
        CancellationToken cancellationToken = default)
    {
        if (append)
            throw new NotSupportedException("Append operations are not supported by the S3 storage backend.");

        ArgumentNullException.ThrowIfNull(content);

        var key = NormalizeKey(path);
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = content,
            AutoCloseStream = false
        };

        await _client.PutObjectAsync(request, cancellationToken);
    }

    public async Task WriteFileAsync(string path, string localFilePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localFilePath);

        await using var stream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            8192, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await WriteAsync(path, stream, false, cancellationToken);
    }

    public async Task WriteTextAsync(string path, string contents, CancellationToken cancellationToken = default)
    {
        var key = NormalizeKey(path);
        var request = new PutObjectRequest { BucketName = _bucket, Key = key, ContentBody = contents };

        await _client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<string> ReadTextAsync(string path, CancellationToken cancellationToken = default)
    {
        var key = NormalizeKey(path);
        if (string.IsNullOrEmpty(key))
            throw new InvalidOperationException("Cannot read the storage root as text.");

        try
        {
            using var response = await _client.GetObjectAsync(_bucket, key, cancellationToken);
            using var reader = response.ResponseStream is null
                ? new StreamReader(Stream.Null)
                : new StreamReader(response.ResponseStream);
            return await reader.ReadToEndAsync(cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException($"Storage object '{path}' does not exist.", ex);
        }
    }

    public async Task<IReadOnlyList<StorageItem>> ListAsync(string path, bool useFlatListing = false,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeKey(path);

        if (!string.IsNullOrEmpty(normalized))
        {
            var meta = await TryGetObjectMetadataAsync(normalized, cancellationToken);
            if (meta is not null)
            {
                StorageItem[] single =
                [
                    new StorageItem(normalized, GetLeafName(normalized), false,
                        meta.ContentLength, meta.LastModified)
                ];
                return single;
            }
        }

        var prefix = useFlatListing
            ? normalized
            : EnsureDirectoryPrefix(normalized);

        List<StorageItem> results = [];
        var request = new ListObjectsV2Request
        {
            BucketName = _bucket,
            Prefix = prefix,
            Delimiter = useFlatListing ? null : "/"
        };

        do
        {
            var response = await _client.ListObjectsV2Async(request, cancellationToken);

            if (!useFlatListing && response.CommonPrefixes is { } prefixes)
            {
                results.AddRange(from commonPrefix in prefixes
                                 select StoragePath.Normalize(commonPrefix)
                    into directoryKey
                                 let name = GetLeafName(directoryKey)
                                 select new StorageItem(directoryKey, name, true));
            }

            if (response.S3Objects is { } objects)
            {
                results.AddRange(from obj in objects
                                 where useFlatListing || !obj.Key.EndsWith('/')
                                 where useFlatListing || !string.Equals(obj.Key, prefix, StringComparison.Ordinal)
                                 let itemKey = StoragePath.Normalize(obj.Key)
                                 let name = GetLeafName(itemKey)
                                 select new StorageItem(itemKey, name, false, obj.Size, obj.LastModified));
            }

            request.ContinuationToken = response.IsTruncated is true ? response.NextContinuationToken : null;
        } while (!string.IsNullOrEmpty(request.ContinuationToken));

        return results;
    }

    public void Dispose() => _client.Dispose();

    async Task<GetObjectMetadataResponse?> TryGetObjectMetadataAsync(string key,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _client.GetObjectMetadataAsync(_bucket, key, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    async IAsyncEnumerable<string> EnumerateKeysAsync(string prefix,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(prefix))
            yield break;

        var request = new ListObjectsV2Request { BucketName = _bucket, Prefix = prefix };

        do
        {
            var response = await _client.ListObjectsV2Async(request, cancellationToken);

            if (response.S3Objects is { } objects)
            {
                foreach (var obj in objects)
                    yield return obj.Key;
            }

            request.ContinuationToken = response.IsTruncated is true ? response.NextContinuationToken : null;
        } while (!string.IsNullOrEmpty(request.ContinuationToken));
    }

    static string NormalizeKey(string path) => StoragePath.Normalize(path);

    static string EnsureDirectoryPrefix(string key)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        return key.EndsWith('/') ? key : key + '/';
    }

    static string GetLeafName(string key)
    {
        var segments = StoragePath.Split(key).ToArray();
        return segments.Length == 0 ? string.Empty : segments[^1];
    }

    sealed class S3ObjectReadStream(GetObjectResponse? response) : Stream
    {
        Stream Inner => response?.ResponseStream ?? Null;

        public override bool CanRead => Inner.CanRead;
        public override bool CanSeek => Inner.CanSeek;
        public override bool CanWrite => Inner.CanWrite;
        public override long Length => Inner.Length;

        public override long Position
        {
            get => Inner.Position;
            set => Inner.Position = value;
        }

        public override void Flush() => Inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => Inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => Inner.Seek(offset, origin);
        public override void SetLength(long value) => Inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => Inner.Write(buffer, offset, count);

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
            CancellationToken cancellationToken = default) =>
            await Inner.ReadAsync(buffer, cancellationToken);

        public override async ValueTask DisposeAsync()
        {
            await Inner.DisposeAsync();
            response?.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Inner.Dispose();
                response?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
