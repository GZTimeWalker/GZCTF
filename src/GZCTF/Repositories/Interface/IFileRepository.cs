namespace GZCTF.Repositories.Interface;

public interface IFileRepository : IRepository
{
    /// <summary>
    /// 创建或更新一个文件
    /// </summary>
    /// <param name="file">文件对象</param>
    /// <param name="fileName">保存文件名</param>
    /// <param name="token">取消Token</param>
    /// <returns>文件Id</returns>
    public Task<LocalFile> CreateOrUpdateFile(IFormFile file, string? fileName = null,
        CancellationToken token = default);

    /// <summary>
    /// 创建或更新一个图像文件
    /// </summary>
    /// <param name="file">文件对象</param>
    /// <param name="fileName">保存文件名</param>
    /// <param name="resize">缩放后的宽，设置为 0 则不缩放</param>
    /// <param name="token">取消Token</param>
    /// <returns>文件Id</returns>
    public Task<LocalFile?> CreateOrUpdateImage(IFormFile file, string fileName, int resize = 300,
        CancellationToken token = default);

    /// <summary>
    /// 删除一个文件
    /// </summary>
    /// <param name="file">文件对象</param>
    /// <param name="token">取消Token</param>
    /// <returns>任务状态</returns>
    public Task<TaskStatus> DeleteFile(LocalFile file, CancellationToken token = default);

    /// <summary>
    /// 根据哈希删除一个文件
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
    public Task<LocalFile?> GetFileByHash(string? fileHash, CancellationToken token = default);

    /// <summary>
    /// 获取全部文件
    /// </summary>
    /// <param name="count">数量</param>
    /// <param name="skip">跳过</param>
    /// <param name="token">取消Token</param>
    /// <returns>文件对象列表</returns>
    public Task<LocalFile[]> GetFiles(int count, int skip, CancellationToken token = default);

    /// <summary>
    /// 删除一个附件
    /// </summary>
    /// <param name="attachment"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task DeleteAttachment(Attachment? attachment, CancellationToken token = default);
}
