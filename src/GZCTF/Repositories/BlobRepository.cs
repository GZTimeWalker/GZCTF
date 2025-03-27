using System.Security.Cryptography;
using FluentStorage;
using FluentStorage.Blobs;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Processing;

namespace GZCTF.Repositories;

public class BlobRepository(AppDbContext context, ILogger<BlobRepository> logger, IBlobStorage storage)
    : RepositoryBase(context), IBlobRepository
{
    public override Task<int> CountAsync(CancellationToken token = default) =>
        Context.Files.CountAsync(token);

    public async Task<LocalFile> CreateOrUpdateBlob(IFormFile file, string? fileName = null,
        CancellationToken token = default)
    {
        await using var tmp = GetTempStream(file.Length);

        logger.SystemLog(
            StaticLocalizer[nameof(Resources.Program.FileRepository_CacheLocation),
                tmp.GetType()], TaskStatus.Pending,
            LogLevel.Trace);

        await file.CopyToAsync(tmp, token);
        return await StoreBlob(fileName ?? file.FileName, tmp, token);
    }

    public async Task<LocalFile?> CreateOrUpdateImage(IFormFile file, string fileName,
        int resize = 300,
        CancellationToken token = default)
    {
        // we do not process images larger than 32MB
        if (file.Length >= 32 * 1024 * 1024)
            return null;

        try
        {
            await using Stream webpStream = new MemoryStream();

            await using (var tmp = GetTempStream(file.Length))
            {
                await file.CopyToAsync(tmp, token);
                tmp.Position = 0;
                using var image = await Image.LoadAsync(tmp, token);

                if (image.Metadata.DecodedImageFormat is GifFormat)
                    return await StoreBlob($"{fileName}.gif", tmp, token);

                if (resize > 0)
                    image.Mutate(im => im.Resize(resize, 0));

                await image.SaveAsWebpAsync(webpStream, token);
            }

            return await StoreBlob($"{fileName}.webp", webpStream, token);
        }
        catch
        {
            logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.FileRepository_ImageSaveFailed),
                    file.Name],
                TaskStatus.Failed, LogLevel.Warning);
            return null;
        }
    }

    public async Task<TaskStatus> DeleteBlob(LocalFile file, CancellationToken token = default)
    {
        var path = StoragePath.Combine(PathHelper.Uploads, file.Location, file.Hash);

        // check if file exists
        if (!await storage.ExistsAsync(path, token))
        {
            Context.Files.Remove(file);
            await SaveAsync(token);
            return TaskStatus.NotFound;
        }

        // check if other ref exists
        if (file.ReferenceCount > 1)
        {
            file.ReferenceCount--; // other ref exists, decrease ref count

            logger.SystemLog(StaticLocalizer[
                    nameof(Resources.Program.FileRepository_ReferenceCounting),
                    file.Hash[..8], file.Name, file.ReferenceCount],
                TaskStatus.Success, LogLevel.Debug);

            await SaveAsync(token);

            return TaskStatus.Success;
        }

        // delete file
        try
        {
            await storage.DeleteAsync(path, token);
        }
        catch (Exception e)
        {
            // log the exception and return failed
            logger.LogErrorMessage(e, StaticLocalizer[
                nameof(Resources.Program.FileRepository_DeleteFile),
                file.Hash[..8], file.Name]);

            return TaskStatus.Failed;
        }

        // delete file record
        Context.Files.Remove(file);
        await SaveAsync(token);

        // log success
        logger.SystemLog(StaticLocalizer[
                nameof(Resources.Program.FileRepository_DeleteFile),
                file.Hash[..8], file.Name],
            TaskStatus.Success, LogLevel.Information);

        return TaskStatus.Success;
    }

    public async Task<TaskStatus> DeleteBlobByHash(string fileHash,
        CancellationToken token = default)
    {
        var file = await GetBlobByHash(fileHash, token);

        if (file is null)
            return TaskStatus.NotFound;

        return await DeleteBlob(file, token);
    }

    public Task<LocalFile?> GetBlobByHash(string? fileHash, CancellationToken token = default)
    {
        if (fileHash is null)
            return Task.FromResult<LocalFile?>(null);

        return Context.Files.SingleOrDefaultAsync(e => e.Hash == fileHash, token);
    }

    public Task<LocalFile[]> GetBlobs(int count, int skip, CancellationToken token = default) =>
        Context.Files.OrderBy(e => e.Name).Skip(skip).Take(count).ToArrayAsync(token);

    public async Task DeleteAttachment(Attachment? attachment, CancellationToken token = default)
    {
        switch (attachment)
        {
            case null:
                return;
            case { Type: FileType.Local, LocalFile: not null }:
                await DeleteBlob(attachment.LocalFile, token);
                break;
        }

        Context.Remove(attachment);
    }

    static Stream GetTempStream(long bufferSize)
    {
        if (bufferSize <= 16 * 1024 * 1024)
            return new MemoryStream();

        return File.Create(Path.GetTempFileName(), 4096, FileOptions.DeleteOnClose);
    }

    async Task<LocalFile> StoreBlob(string fileName, Stream contentStream,
        CancellationToken token = default)
    {
        contentStream.Position = 0;
        var hash = await SHA256.HashDataAsync(contentStream, token);
        var fileHash = Convert.ToHexStringLower(hash);

        var localFile = await GetBlobByHash(fileHash, token);

        if (localFile is not null)
        {
            localFile.FileSize = contentStream.Length;
            localFile.Name = fileName; // allow to rename
            localFile.UploadTimeUtc = DateTimeOffset.UtcNow; // update upload time
            localFile.ReferenceCount++; // same hash, add ref count

            logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.FileRepository_ReferenceCounting),
                    localFile.Hash[..8], localFile.Name,
                    localFile.ReferenceCount],
                TaskStatus.Success, LogLevel.Debug);

            Context.Update(localFile);
        }
        else
        {
            localFile = new() { Hash = fileHash, Name = fileName, FileSize = contentStream.Length };
            await Context.AddAsync(localFile, token);
        }

        var path = StoragePath.Combine(PathHelper.Uploads, localFile.Location, localFile.Hash);

        contentStream.Position = 0;
        await storage.WriteAsync(path, contentStream, false, token);

        await SaveAsync(token);
        return localFile;
    }
}
