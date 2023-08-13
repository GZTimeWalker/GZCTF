using System.Security.Cryptography;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.Formats.Gif;

namespace GZCTF.Repositories;

public class FileRepository : RepositoryBase, IFileRepository
{
    private readonly ILogger<FileRepository> _logger;
    private readonly string _uploadPath;

    public FileRepository(AppDbContext context, IConfiguration configuration,
        ILogger<FileRepository> logger) : base(context)
    {
        _logger = logger;
        _uploadPath = configuration.GetSection("UploadFolder")?.Value ?? "uploads";
    }

    public override Task<int> CountAsync(CancellationToken token = default) => _context.Files.CountAsync(token);

    private static Stream GetStream(long bufferSize) => bufferSize <= 16 * 1024 * 1024 ? new MemoryStream() :
                File.Create(Path.GetTempFileName(), 4096, FileOptions.DeleteOnClose);

    private async Task<LocalFile> StoreLocalFile(string fileName, Stream contentStream, CancellationToken token = default)
    {
        contentStream.Position = 0;
        var hash = await SHA256.HashDataAsync(contentStream, token);
        var fileHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

        var localFile = await GetFileByHash(fileHash, token);

        if (localFile is not null)
        {
            localFile.FileSize = contentStream.Length;
            localFile.Name = fileName; // allow rename
            localFile.UploadTimeUTC = DateTimeOffset.UtcNow; // update upload time
            localFile.ReferenceCount++; // same hash, add ref count

            _logger.SystemLog($"文件引用计数 [{localFile.Hash[..8]}] {localFile.Name} => {localFile.ReferenceCount}", TaskStatus.Success, LogLevel.Debug);

            _context.Update(localFile);
        }
        else
        {
            localFile = new() { Hash = fileHash, Name = fileName, FileSize = contentStream.Length };
            await _context.AddAsync(localFile, token);

            var path = Path.Combine(_uploadPath, localFile.Location);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            using var fileStream = File.Create(Path.Combine(path, localFile.Hash));

            contentStream.Position = 0;
            await contentStream.CopyToAsync(fileStream, token);
        }

        await SaveAsync(token);
        return localFile;
    }

    public async Task<LocalFile> CreateOrUpdateFile(IFormFile file, string? fileName = null, CancellationToken token = default)
    {
        using var tmp = GetStream(file.Length);

        _logger.SystemLog($"缓存位置：{tmp.GetType()}", TaskStatus.Pending, LogLevel.Trace);

        await file.CopyToAsync(tmp, token);
        return await StoreLocalFile(fileName ?? file.FileName, tmp, token);
    }

    public async Task<LocalFile?> CreateOrUpdateImage(IFormFile file, string fileName, CancellationToken token = default, int resize = 300)
    {
        // we do not process images larger than 32MB
        if (file.Length >= 32 * 1024 * 1024)
            return null;

        try
        {
            using Stream webpStream = new MemoryStream();

            using (var tmp = GetStream(file.Length))
            {
                await file.CopyToAsync(tmp, token);
                tmp.Position = 0;
                using var image = await Image.LoadAsync(tmp, token);

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
            _logger.SystemLog($"无法存储图像文件：{file.Name}", TaskStatus.Failed, LogLevel.Warning);
            return null;
        }
    }

    public async Task<TaskStatus> DeleteFile(LocalFile file, CancellationToken token = default)
    {
        var path = Path.Combine(_uploadPath, file.Location, file.Hash);

        if (file.ReferenceCount > 1)
        {
            file.ReferenceCount--; // other ref exists, decrease ref count

            _logger.SystemLog($"文件引用计数 [{file.Hash[..8]}] {file.Name} => {file.ReferenceCount}", TaskStatus.Success, LogLevel.Debug);

            await SaveAsync(token);

            return TaskStatus.Success;
        }

        _logger.SystemLog($"删除文件 [{file.Hash[..8]}] {file.Name}", TaskStatus.Pending, LogLevel.Information);

        if (File.Exists(path))
        {
            File.Delete(path);
            _context.Files.Remove(file);
            await SaveAsync(token);

            return TaskStatus.Success;
        }

        _context.Files.Remove(file);
        await SaveAsync(token);

        return TaskStatus.NotFound;
    }

    public async Task<TaskStatus> DeleteFileByHash(string fileHash, CancellationToken token = default)
    {
        var file = await GetFileByHash(fileHash, token);

        if (file is null)
            return TaskStatus.NotFound;

        return await DeleteFile(file, token);
    }

    public Task<LocalFile?> GetFileByHash(string? fileHash, CancellationToken token = default)
        => _context.Files.SingleOrDefaultAsync(f => f.Hash == fileHash, token);

    public Task<LocalFile[]> GetFiles(int count, int skip, CancellationToken token = default)
        => _context.Files.OrderBy(e => e.Name).Skip(skip).Take(count).ToArrayAsync(token);
}
