using CTFServer.Models;
using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using CTFServer.Utils;
using NLog;

namespace CTFServer.Repositories;

public class FileRepository : RepositoryBase, IFileRepository
{
    private static readonly Logger logger = LogManager.GetLogger("FileRepository");
    private readonly IConfiguration configuration;
    public FileRepository(AppDbContext context, IConfiguration _configuration) : base(context)
    {
        configuration = _configuration;
    }

    public async Task<LocalFile> CreateOrUpdateFile(IFormFile file, string? fileName = null, CancellationToken token = default)
    {
        using Stream tmp = file.Length <= 16 * 1024 * 1024 ? 
                new MemoryStream() :
                File.Create(Path.GetTempFileName(), 4096, FileOptions.DeleteOnClose);

        LogHelper.SystemLog(logger, $"缓存位置：{tmp.GetType()}");

        await file.CopyToAsync(tmp, token);

        tmp.Position = 0;
        var hash = await SHA256.Create().ComputeHashAsync(tmp, token);
        var fileHash = BitConverter.ToString(hash).Replace("-", "").ToLower();

        var basepath = configuration.GetSection("UploadFolder").Value ?? "uploads";

        var localFile = await GetFileByHash(fileHash, token);

        if(localFile is null)
        {
            localFile = new() { Hash = fileHash, Name = fileName ?? file.FileName };
            await context.AddAsync(localFile, token);
        }
        else
        {
            localFile.Name = fileName ?? file.FileName;
            context.Update(localFile);
        }

        var path = Path.Combine(basepath, localFile.Location);

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        using (var fileStream = File.Create(Path.Combine(path, localFile.Hash)))
        {
            tmp.Position = 0;
            await tmp.CopyToAsync(fileStream, token);
        }

        await context.SaveChangesAsync(token);

        return localFile;
    }

    public async Task<TaskStatus> DeleteFileByHash(string fileHash, CancellationToken token =  default)
    {
        var file = await context.Files.Where(f => f.Hash == fileHash).FirstOrDefaultAsync(token);

        if (file is null)
            return TaskStatus.NotFound;

        var basepath = configuration.GetSection("UploadFolder").Value ?? "uploads";
        var path = Path.Combine(basepath, file.Location, file.Hash);

        LogHelper.SystemLog(logger, $"正在删除文件 [{file.Hash[..8]}] {file.Name}...", TaskStatus.Pending, NLog.LogLevel.Info);

        if (File.Exists(path))
        {
            File.Delete(path);
            context.Files.Remove(file);
            await context.SaveChangesAsync(token);

            return TaskStatus.Success;
        }
        else
        {
            context.Files.Remove(file);
            await context.SaveChangesAsync(token);

            return TaskStatus.NotFound;
        }
    }

    public Task<LocalFile?> GetFileByHash(string fileHash, CancellationToken token =  default)
        => context.Files.Where(f => f.Hash == fileHash).FirstOrDefaultAsync(token);

    public Task<LocalFile?> GetFilesById(int fileId, CancellationToken token =  default)
        => context.Files.Where(f => f.Id == fileId).FirstOrDefaultAsync(token);

    public Task<List<LocalFile>> GetFilesByName(string fileName, CancellationToken token =  default)
        => context.Files.Where(f => f.Name == fileName).ToListAsync(token);
}
