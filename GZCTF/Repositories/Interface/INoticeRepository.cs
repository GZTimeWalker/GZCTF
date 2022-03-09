using CTFServer.Models;

namespace CTFServer.Repositories.Interface;

public interface INoticeRepository
{
    /// <summary>
    /// 获取指定数量的公告
    /// </summary>
    /// <param name="skip">跳过数量</param>
    /// <param name="count">数量</param>
    /// <param name="token">操作取消token</param>
    /// <returns>不超过指定数量的公告</returns>
    public Task<List<Notice>> GetNotices(int skip, int count, CancellationToken token);

    /// <summary>
    /// 通过Id获取公告
    /// </summary>
    /// <param name="Id">公告Id</param>
    /// <param name="token">操作取消token</param>
    /// <returns></returns>
    public Task<Notice?> GetNoticeById(int Id, CancellationToken token);

    /// <summary>
    /// 添加或更新公告
    /// </summary>
    /// <param name="notice">公告对象</param>
    /// <param name="token">操作取消token</param>
    /// <returns></returns>
    public Task<Notice> AddOrUpdateNotice(Notice notice, CancellationToken token);

    /// <summary>
    /// 删除一条公告
    /// </summary>
    /// <param name="notice">公告对象</param>
    /// <param name="token">操作取消token</param>
    /// <returns></returns>
    public Task RemoveNotice(Notice notice, CancellationToken token);
}
