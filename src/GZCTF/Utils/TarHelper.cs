using System.Formats.Tar;
using System.IO.Compression;
using System.Web;
using FluentStorage;
using FluentStorage.Blobs;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Utils;

public static class TarHelper
{
    static void SetHeaders(HttpContext context, string fileName)
    {
        var downloadFilename = fileName.EndsWith(".tar.gz") ? fileName : $"{fileName}.tar.gz";

        context.Response.ContentType = "application/gzip";
        context.Response.Headers.ContentDisposition =
            $"attachment; filename=\"{HttpUtility.UrlEncode(downloadFilename)}\"";
    }

    /// <summary>
    /// Archive files to a tarball
    /// </summary>
    /// <param name="context">HTTP Context</param>
    /// <param name="storage">Blob Storage Backend</param>
    /// <param name="files">Files to archive</param>
    /// <param name="basePath">The base path for files</param>
    /// <param name="fileName">The archive file name</param>
    /// <param name="token"></param>
    public static async Task SendFilesAsync(
        HttpContext context,
        IBlobStorage storage,
        IEnumerable<LocalFile> files, string basePath, string fileName,
        CancellationToken token = default
    )
    {
        SetHeaders(context, fileName);

        await using var compress = new GZipStream(context.Response.Body, CompressionMode.Compress);
        await using var writer = new TarWriter(compress);

        foreach (LocalFile file in files)
        {
            var filePath = StoragePath.Combine(basePath, file.Location, file.Hash);
            var entryPath = $"{fileName}/{file.Name}";
            var blob = await storage.GetBlobAsync(filePath, token);
            await using var stream = await GetFileStream(storage, blob, token);
            var entry = new PaxTarEntry(TarEntryType.RegularFile, entryPath)
            {
                DataStream = stream,
                ModificationTime = blob.LastModificationTime ?? DateTimeOffset.Now
            };
            await writer.WriteEntryAsync(entry, token);
        }
    }

    /// <summary>
    /// Archive a directory to a tarball
    /// </summary>
    /// <param name="context">HTTP Context</param>
    /// <param name="directory">Directory to archive</param>
    /// <param name="storage">Blob Storage Backend</param>
    /// <param name="fileName">The archive file name</param>
    /// <param name="token"></param>
    public static async Task SendDirectoryAsync(
        HttpContext context,
        IBlobStorage storage,
        string directory, string fileName,
        CancellationToken token = default
    )
    {
        SetHeaders(context, fileName);

        await using var compress = new GZipStream(context.Response.Body, CompressionMode.Compress);
        await using var writer = new TarWriter(compress);

        var files = await storage.ListAsync(directory, cancellationToken: token);
        foreach (Blob blob in files)
        {
            var entryPath = $"{fileName}/{blob.Name}";

            await using var stream = await GetFileStream(storage, blob, token);
            var entry = new PaxTarEntry(TarEntryType.RegularFile, entryPath)
            {
                DataStream = stream,
                ModificationTime = blob.LastModificationTime ?? DateTimeOffset.Now
            };
            await writer.WriteEntryAsync(entry, token);
        }
    }

    /// <summary>
    /// Get a file stream from the blob storage
    /// </summary>
    /// <param name="storage">Blob Storage Backend</param>
    /// <param name="fileBlob">File blob</param>
    /// <param name="token"></param>
    /// <returns></returns>
    static async Task<Stream> GetFileStream(IBlobStorage storage, Blob fileBlob,
        CancellationToken token)
    {
        var sourceStream = await storage.OpenReadAsync(fileBlob.FullPath, token);

        if (sourceStream.CanSeek)
            return sourceStream;

        Stream stream = BufferHelper.GetTempStream(fileBlob.Size);
        try
        {
            await sourceStream.CopyToAsync(stream, token);
            stream.Position = 0;
            return stream;
        }
        finally
        {
            await sourceStream.DisposeAsync();
        }
    }
}

public class TarFilesResult(
    IBlobStorage storage,
    IEnumerable<LocalFile> files,
    string basePath,
    string fileName,
    CancellationToken token)
    : IActionResult
{
    public async Task ExecuteResultAsync(ActionContext context)
    {
        try
        {
            await TarHelper.SendFilesAsync(context.HttpContext, storage, files, basePath, fileName, token);
        }
        catch (OperationCanceledException)
        {
            context.HttpContext.Abort();
        }
    }
}

public class TarDirectoryResult(IBlobStorage storage, string directory, string fileName, CancellationToken token)
    : IActionResult
{
    public async Task ExecuteResultAsync(ActionContext context)
    {
        try
        {
            await TarHelper.SendDirectoryAsync(context.HttpContext, storage, directory, fileName, token);
        }
        catch (OperationCanceledException)
        {
            context.HttpContext.Abort();
        }
    }
}
