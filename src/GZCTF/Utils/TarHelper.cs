using System.Formats.Tar;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace GZCTF.Utils;

public static class TarHelper
{
    static void SetHeaders(HttpContext context, string fileName)
    {
        var downloadFilename = fileName.EndsWith(".tar") ? fileName : $"{fileName}.tar";
        
        context.Response.ContentType = "application/x-tar";
        context.Response.Headers.ContentDisposition = $"attachment; filename=\"{downloadFilename}\"";
    }

    /// <summary>
    /// Archive files to a tarball
    /// </summary>
    /// <param name="context">HTTP Context</param>
    /// <param name="files">Files to archive</param>
    /// <param name="basePath">The base path for files</param>
    /// <param name="fileName">The archive file name</param>
    /// <param name="token"></param>
    public static async Task SendFilesAsync(
        HttpContext context,
        IEnumerable<LocalFile> files, string basePath, string fileName,
        CancellationToken token = default
    )
    {
        SetHeaders(context, fileName);
        
        await using var compression = new GZipStream(context.Response.Body, CompressionMode.Compress);
        await using var tar = new TarWriter(compression);

        foreach (var file in files)
        {
            var filePath = Path.Combine(basePath, file.Location, file.Hash);
            var entry = Path.Combine(fileName, file.Name);
            await tar.WriteEntryAsync(filePath, entry, token);
        }
    }

    /// <summary>
    /// Archive a directory to a tarball
    /// </summary>
    /// <param name="context">HTTP Context</param>
    /// <param name="directory">Directory to archive</param>
    /// <param name="fileName">The archive file name</param>
    /// <param name="token"></param>
    public static async Task SendDirectoryAsync(
        HttpContext context,
        string directory, string fileName,
        CancellationToken token = default
    )
    {
        SetHeaders(context, fileName);

        await using var compression = new GZipStream(context.Response.Body, CompressionMode.Compress);
        await using var tar = new TarWriter(compression);

        var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var entry = Path.Combine(fileName, Path.GetRelativePath(directory, file));
            await tar.WriteEntryAsync(file, entry, token);
        }
    }
}

public class TarFilesResult(
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
            await TarHelper.SendFilesAsync(context.HttpContext, files, basePath, fileName, token);
        }
        catch (OperationCanceledException)
        {
            context.HttpContext.Abort();
        }
    }
}

public class TarDirectoryResult(string directory, string fileName, CancellationToken token)
    : IActionResult
{
    public async Task ExecuteResultAsync(ActionContext context)
    {
        try
        {
            await TarHelper.SendDirectoryAsync(context.HttpContext, directory, fileName, token);
        }
        catch (OperationCanceledException)
        {
            context.HttpContext.Abort();
        }
    }
}
