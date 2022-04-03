using CTFServer.Models;
using CTFServer.Models.Request.Game;

namespace CTFServer.Repositories.Interface;

public interface IGameRepository : IRepository
{
    /// <summary>
    /// 获取指定数量的比赛对象
    /// </summary>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<List<Game>> GetGames(int count = 10, int skip = 0, CancellationToken token = default);

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
    /// 更新比赛信息
    /// </summary>
    /// <param name="game">比赛对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<int> UpdateGame(Game game, CancellationToken token = default);

    /// <summary>
    /// 获取排行榜
    /// </summary>
    /// <param name="game">比赛对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Scoreboard> GetScoreboard(Game game, CancellationToken token = default);
}
