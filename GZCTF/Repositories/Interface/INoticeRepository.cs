using CTFServer.Models;

namespace CTFServer.Repositories.Interface;

public interface INoticeRepository
{
    /// <summary>
    /// 添加公告
    /// </summary>
    /// <param name="notice">公告信息</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Notice> AddNotice(Notice notice, CancellationToken token = default);
}
