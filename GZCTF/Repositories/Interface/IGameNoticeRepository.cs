using CTFServer.Models;

namespace CTFServer.Repositories.Interface;

public interface IGameNoticeRepository : IRepository
{
    /// <summary>
    /// 添加一个通知
    /// </summary>
    /// <param name="game">比赛对象</param>
    /// <param name="notice">通知</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameNotice> AddNotice(Game game, GameNotice notice, CancellationToken token = default);

    /// <summary>
    /// 获取全部通知
    /// </summary>
    /// <param name="game">比赛对象</param>
    /// <param name="count">数量</param>
    /// <param name="skip">跳过数量</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameNotice[]> GetNotices(Game game, int count = 10, int skip = 0, CancellationToken token = default);
}
