using CTFServer.Models;

namespace CTFServer.Repositories.Interface;

public interface IFileRepository
{
    /// <summary>
    /// 创建或更新一个文件
    /// </summary>
    /// <param name="file">文件对象</param>
    /// <param name="fileName">保存文件名</param>
    /// <param name="token">取消Token</param>
    /// <returns>文件Id</returns>
    public Task<LocalFile> CreateOrUpdateFile(IFormFile file, string? fileName = null, CancellationToken token = default);

    /// <summary>
    /// 删除一个文件
    /// </summary>
    /// <param name="fileHash">文件哈希</param>
    /// <param name="token">取消Token</param>
    /// <returns>任务状态</returns>
    public Task<TaskStatus> DeleteFileByHash(string fileHash, CancellationToken token = default);

    /// <summary>
    /// 根据文件哈希获取文件
    /// </summary>
    /// <param name="fileHash">文件哈希</param>
    /// <param name="token">取消Token</param>
    /// <returns>文件对象</returns>
    public Task<LocalFile?> GetFileByHash(string fileHash, CancellationToken token = default);

    /// <summary>
    /// 根据文件名称获取文件
    /// </summary>
    /// <param name="fileName">文件名称</param>
    /// <param name="token">取消Token</param>
    /// <returns>文件对象列表</returns>
    public Task<List<LocalFile>> GetFilesByName(string fileName, CancellationToken token = default);

    /// <summary>
    /// 根据Id称获取文件
    /// </summary>
    /// <param name="fileId">文件名称</param>
    /// <param name="token">取消Token</param>
    /// <returns>文件对象列表</returns>
    public Task<LocalFile?> GetFilesById(int fileId, CancellationToken token = default);
}
