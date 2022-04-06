using CTFServer.Models;

namespace CTFServer.Repositories.Interface;

public interface INoticeRepository : IRepository
{
    /// <summary>
    /// 添加公告
    /// </summary>
    /// <param name="notice">公告对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Notice> AddNotice(Notice notice, CancellationToken token = default);

    /// <summary>
    /// 移除公告
    /// </summary>
    /// <param name="notice">公告对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task RemoveNotice(Notice notice, CancellationToken token = default);

    /// <summary>
    /// 获取全部公告
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Notice[]> GetNotices(CancellationToken token = default);

    /// <summary>
    /// 根据 Id 返回公告
    /// </summary>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Notice?> GetNoticeById(int id, CancellationToken token = default);

    /// <summary>
    /// 异步更新
    /// </summary>
    /// <param name="notice">公告对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<int> UpdateAsync(Notice notice, CancellationToken token = default);
}
