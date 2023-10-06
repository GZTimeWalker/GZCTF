using System.Security.Cryptography;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.Formats.Gif;

namespace GZCTF.Repositories;

public class FileRepository(AppDbContext context, ILogger<FileRepository> logger) : RepositoryBase(context),
    IFileRepository
{
    public override Task<int> CountAsync(CancellationToken token = default) => context.Files.CountAsync(token);

    public async Task<LocalFile> CreateOrUpdateFile(IFormFile file, string? fileName = null,
        CancellationToken token = default)
    {
        using Stream tmp = GetStream(file.Length);

        logger.SystemLog($"缓存位置：{tmp.GetType()}", TaskStatus.Pending, LogLevel.Trace);

        await file.CopyToAsync(tmp, token);
        return await StoreLocalFile(fileName ?? file.FileName, tmp, token);
    }

    public async Task<LocalFile?> CreateOrUpdateImage(IFormFile file, string fileName, int resize = 300,
        CancellationToken token = default)
    {
        // we do not process images larger than 32MB
        if (file.Length >= 32 * 1024 * 1024)
            return null;

        try
        {
            using Stream webpStream = new MemoryStream();

            using (Stream tmp = GetStream(file.Length))
            {
                await file.CopyToAsync(tmp, token);
                tmp.Position = 0;
                using Image image = await Image.LoadAsync(tmp, token);

                if (image.Metadata.DecodedImageFormat is GifFormat)
                    return await StoreLocalFile($"{fileName}.gif", tmp, token);

                if (resize > 0)
                    image.Mutate(im => im.Resize(resize, 0));

                await image.SaveAsWebpAsync(webpStream, token);
            }

            return await StoreLocalFile($"{fileName}.webp", webpStream, token);
        }
        catch
        {
            logger.SystemLog($"无法存储图像文件：{file.Name}", TaskStatus.Failed, LogLevel.Warning);
            return null;
        }
    }

    public async Task<TaskStatus> DeleteFile(LocalFile file, CancellationToken token = default)
    {
        var path = Path.Combine(FilePath.Uploads, file.Location, file.Hash);

        if (file.ReferenceCount > 1)
        {
            file.ReferenceCount--; // other ref exists, decrease ref count

            logger.SystemLog($"文件引用计数 [{file.Hash[..8]}] {file.Name} => {file.ReferenceCount}", TaskStatus.Success,
                LogLevel.Debug);

            await SaveAsync(token);

            return TaskStatus.Success;
        }

        logger.SystemLog($"删除文件 [{file.Hash[..8]}] {file.Name}", TaskStatus.Pending, LogLevel.Information);

        if (File.Exists(path))
        {
            File.Delete(path);
            context.Files.Remove(file);
            await SaveAsync(token);

            return TaskStatus.Success;
        }

        context.Files.Remove(file);
        await SaveAsync(token);

        return TaskStatus.NotFound;
    }

    public async Task<TaskStatus> DeleteFileByHash(string fileHash, CancellationToken token = default)
    {
        LocalFile? file = await GetFileByHash(fileHash, token);

        if (file is null)
            return TaskStatus.NotFound;

        return await DeleteFile(file, token);
    }

    public Task<LocalFile?> GetFileByHash(string? fileHash, CancellationToken token = default) =>
        context.Files.SingleOrDefaultAsync(f => f.Hash == fileHash, token);

    public Task<LocalFile[]> GetFiles(int count, int skip, CancellationToken token = default) =>
        context.Files.OrderBy(e => e.Name).Skip(skip).Take(count).ToArrayAsync(token);

    static Stream GetStream(long bufferSize) =>
        bufferSize <= 16 * 1024 * 1024
            ? new MemoryStream()
            : File.Create(Path.GetTempFileName(), 4096, FileOptions.DeleteOnClose);

    async Task<LocalFile> StoreLocalFile(string fileName, Stream contentStream, CancellationToken token = default)
    {
        contentStream.Position = 0;
        var hash = await SHA256.HashDataAsync(contentStream, token);
        var fileHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

        LocalFile? localFile = await GetFileByHash(fileHash, token);

        if (localFile is not null)
        {
            localFile.FileSize = contentStream.Length;
            localFile.Name = fileName; // allow rename
            localFile.UploadTimeUTC = DateTimeOffset.UtcNow; // update upload time
            localFile.ReferenceCount++; // same hash, add ref count

            logger.SystemLog($"文件引用计数 [{localFile.Hash[..8]}] {localFile.Name} => {localFile.ReferenceCount}",
                TaskStatus.Success, LogLevel.Debug);

            context.Update(localFile);
        }
        else
        {
            localFile = new() { Hash = fileHash, Name = fileName, FileSize = contentStream.Length };
            await context.AddAsync(localFile, token);
        }

        var path = Path.Combine(FilePath.Uploads, localFile.Location);

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        using FileStream fileStream = File.Create(Path.Combine(path, localFile.Hash));

        contentStream.Position = 0;
        await contentStream.CopyToAsync(fileStream, token);

        await SaveAsync(token);
        return localFile;
    }
}
