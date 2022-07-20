using CTFServer.Models.Request.Game;

namespace CTFServer.Repositories.Interface;

public interface IGameRepository : IRepository
{
    /// <summary>
    /// 获取指定数量的比赛对象基本信息
    /// </summary>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<BasicGameInfoModel[]> GetBasicGameInfo(int count = 10, int skip = 0, CancellationToken token = default);

    /// <summary>
    /// 获取指定数量的比赛对象
    /// </summary>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Game[]> GetGames(int count = 10, int skip = 0, CancellationToken token = default);

    /// <summary>
    /// 根据Id获取比赛对象
    /// </summary>
    /// <param name="id">比赛Id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Game?> GetGameById(int id, CancellationToken token = default);

    /// <summary>
    /// 创建比赛对象
    /// </summary>
    /// <param name="game">比赛对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Game?> CreateGame(Game game, CancellationToken token = default);

    /// <summary>
    /// 获取排行榜
    /// </summary>
    /// <param name="game">比赛对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<ScoreboardModel> GetScoreboard(Game game, CancellationToken token = default);

    /// <summary>
    /// 刷新排行榜
    /// </summary>
    /// <param name="game">比赛对象</param>
    public void FlushScoreboard(Game game);

    /// <summary>
    /// 刷新比赛信息缓存
    /// </summary>
    public void FlushGameInfoCache();
}