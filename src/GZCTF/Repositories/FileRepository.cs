﻿using System.Security.Cryptography;
using CTFServer.Repositories.Interface;
using CTFServer.Utils;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Repositories;

public class FileRepository : RepositoryBase, IFileRepository
{
    private readonly ILogger<FileRepository> logger;
    private readonly string uploadPath;

    public FileRepository(AppDbContext context, IConfiguration _configuration,
        ILogger<FileRepository> _logger) : base(context)
    {
        logger = _logger;
        uploadPath = _configuration.GetSection("UploadFolder")?.Value ?? "uploads";
    }

    public override Task<int> CountAsync(CancellationToken token = default) => context.Files.CountAsync(token);

    public async Task<LocalFile> CreateOrUpdateFile(IFormFile file, string? fileName = null, CancellationToken token = default)
    {
        using Stream tmp = file.Length <= 16 * 1024 * 1024 ? new MemoryStream() :
                File.Create(Path.GetTempFileName(), 4096, FileOptions.DeleteOnClose);

        logger.SystemLog($"缓存位置：{tmp.GetType()}", TaskStatus.Pending, LogLevel.Trace);

        await file.CopyToAsync(tmp, token);

        tmp.Position = 0;
        var hash = await SHA256.HashDataAsync(tmp, token);
        var fileHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

        var localFile = await GetFileByHash(fileHash, token);

        if (localFile is not null)
        {
            localFile.FileSize = file.Length;
            localFile.Name = fileName ?? file.FileName; // allow rename
            localFile.UploadTimeUTC = DateTimeOffset.UtcNow; // update upload time
            localFile.ReferenceCount++; // same hash, add ref count

            logger.SystemLog($"文件引用计数 [{localFile.Hash[..8]}] {localFile.Name} => {localFile.ReferenceCount}", TaskStatus.Success, LogLevel.Debug);

            context.Update(localFile);
        }
        else
        {
            localFile = new() { Hash = fileHash, Name = fileName ?? file.FileName, FileSize = file.Length };
            await context.AddAsync(localFile, token);

            var path = Path.Combine(uploadPath, localFile.Location);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            using var fileStream = File.Create(Path.Combine(path, localFile.Hash));

            tmp.Position = 0;
            await tmp.CopyToAsync(fileStream, token);
        }

        await SaveAsync(token);

        return localFile;
    }

    public async Task<TaskStatus> DeleteFile(LocalFile file, CancellationToken token = default)
    {
        var path = Path.Combine(uploadPath, file.Location, file.Hash);

        if (file.ReferenceCount > 1)
        {
            file.ReferenceCount--; // other ref exists, decrease ref count

            logger.SystemLog($"文件引用计数 [{file.Hash[..8]}] {file.Name} => {file.ReferenceCount}", TaskStatus.Success, LogLevel.Debug);

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
        var file = await GetFileByHash(fileHash, token);

        if (file is null)
            return TaskStatus.NotFound;

        return await DeleteFile(file, token);
    }

    public Task<LocalFile?> GetFileByHash(string? fileHash, CancellationToken token = default)
        => context.Files.SingleOrDefaultAsync(f => f.Hash == fileHash, token);

    public Task<List<LocalFile>> GetFiles(int count, int skip, CancellationToken token = default)
        => context.Files.OrderBy(e => e.Name).Skip(skip).Take(count).ToListAsync(token);
}
