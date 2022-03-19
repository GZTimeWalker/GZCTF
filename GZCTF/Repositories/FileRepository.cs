using CTFServer.Models;
using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using CTFServer.Utils;
using NLog;

namespace CTFServer.Repositories;

public class FileRepository : RepositoryBase, IFileRepository
{
    private readonly IConfiguration configuration;
    public FileRepository(AppDbContext context, IConfiguration _configuration) : base(context)
    {
        configuration = _configuration;
    }

    public async Task<string> CreateOrUpdateFile(IFormFile file, CancellationToken token)
    {
        MemoryStream tmp = new();
        await file.CopyToAsync(tmp, token);
        var hash = await SHA256.Create().ComputeHashAsync(tmp, token);

        var basepath = configuration.GetSection("UploadFolder").Value ?? "files";
        var fileHash = BitConverter.ToString(hash).Replace("-", "").ToLower();

        var localFile = await GetFileByHash(fileHash, token);

        if(localFile is null)
        {
            localFile = new() { Hash = fileHash, Name = file.Name };
            await context.AddAsync(localFile, token);
        }
        else
            localFile.Name = file.Name;

        var path = Path.Combine(basepath, localFile.Location);

        if (!Directory.Exists(Path.GetDirectoryName(path)))
            Directory.CreateDirectory(path);

        using (var fileStream = File.Create(Path.Combine(path, localFile.Hash)))
        {
            tmp.Seek(0, SeekOrigin.Begin);
            await tmp.CopyToAsync(fileStream, token);
        }


        await context.SaveChangesAsync(token);

        return localFile.Hash;
    }

    public async Task<TaskStatus> DeleteFileByHash(string fileHash, CancellationToken token)
    {
        var file = await context.Files.Where(f => f.Hash == fileHash).FirstOrDefaultAsync(token);

        if (file is null)
            return TaskStatus.NotFound;

        var basepath = configuration.GetSection("UploadFolder").Value ?? "files";
        var path = Path.Combine(basepath, file.Location);

        if (File.Exists(path))
        {
            File.Delete(path);

            context.Files.Remove(file);
            await context.SaveChangesAsync(token);

            return TaskStatus.Success;
        }
        else
            return TaskStatus.NotFound;
    }

    public Task<LocalFile?> GetFileByHash(string fileHash, CancellationToken token)
        => context.Files.Where(f => f.Hash == fileHash).FirstOrDefaultAsync(token);

    public Task<List<LocalFile>> GetFilesByName(string fileName, CancellationToken token)
        => context.Files.Where(f => f.Name == fileName).ToListAsync(token);
}
