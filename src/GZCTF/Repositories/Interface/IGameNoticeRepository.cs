using GZCTF.Models.Request.Game;

namespace GZCTF.Repositories.Interface;

public interface IGameNoticeRepository : IRepository
{
    /// <summary>
    /// Add a new game notice
    /// </summary>
    /// <param name="notice"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameNotice> AddNotice(GameNotice notice, CancellationToken token = default);

    /// <summary>
    /// Get latest notices for a specific game
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<DataWithModifiedTime<GameNotice[]>> GetLatestNotices(int gameId, CancellationToken token = default);

    /// <summary>
    /// Get all user-editable notices for a specific game
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameNotice[]> GetNormalNotices(int gameId, CancellationToken token = default);

    /// <summary>
    /// Update a game notice
    /// </summary>
    /// <param name="notice"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameNotice> UpdateNotice(GameNotice notice, CancellationToken token = default);

    /// <summary>
    /// Get a specific notice by its ID within a specific game
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="noticeId"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameNotice?> GetNoticeById(int gameId, int noticeId, CancellationToken token = default);

    /// <summary>
    /// Remove a game notice
    /// </summary>
    /// <param name="notice"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task RemoveNotice(GameNotice notice, CancellationToken token = default);
}
