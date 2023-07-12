namespace GZCTF.Repositories.Interface;

public interface IGameNoticeRepository : IRepository
{
    /// <summary>
    /// 添加一个通知
    /// </summary>
    /// <param name="notice">通知</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameNotice> AddNotice(GameNotice notice, CancellationToken token = default);

    /// <summary>
    /// 获取比赛通知
    /// </summary>
    /// <param name="gameId">比赛Id</param>
    /// <param name="count">数量</param>
    /// <param name="skip">跳过数量</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameNotice[]> GetNotices(int gameId, int count = 20, int skip = 0, CancellationToken token = default);

    /// <summary>
    /// 获取比赛常规通知（可编辑的）
    /// </summary>
    /// <param name="gameId">比赛Id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameNotice[]> GetNormalNotices(int gameId, CancellationToken token = default);

    /// <summary>
    /// 更新比赛通知
    /// </summary>
    /// <param name="notice"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameNotice> UpdateNotice(GameNotice notice, CancellationToken token = default);

    /// <summary>
    /// 获取比赛通知
    /// </summary>
    /// <param name="gameId">比赛Id</param>
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
