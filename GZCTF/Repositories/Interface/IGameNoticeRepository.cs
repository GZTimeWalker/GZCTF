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
    public Task<GameNotice> CreateNotice(Game game, GameNotice notice, CancellationToken token = default);

    /// <summary>
    /// 获取比赛通知
    /// </summary>
    /// <param name="gameId">比赛Id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameNotice[]> GetNotices(int gameId, CancellationToken token = default);

    /// <summary>
    /// 获取比赛通知
    /// </summary>
    /// <param name="gameId">比赛ID</param>
    /// <param name="noticeId">通知ID</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameNotice?> GetNoticeById(int gameId, int noticeId, CancellationToken token = default);

    /// <summary>
    /// 删除比赛通知
    /// </summary>
    /// <param name="notice">比赛通知对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task RemoveNotice(GameNotice notice, CancellationToken token = default);
}