using GZCTF.Models.Request.Game;

namespace GZCTF.Repositories.Interface;

public interface IGameRepository : IRepository
{
    /// <summary>
    /// 获取指定数量的比赛对象基本信息
    /// </summary>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<ArrayResponse<BasicGameInfoModel>> GetGameInfo(int count = 10, int skip = 0,
        CancellationToken token = default);

    /// <summary>
    /// 从数据库中获取比赛基本信息
    /// </summary>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<BasicGameInfoModel[]> FetchGameList(int count, int skip, CancellationToken token);

    /// <summary>
    /// 获取指定数量的比赛对象
    /// </summary>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Game[]> GetGames(int count = 10, int skip = 0, CancellationToken token = default);

    /// <summary>
    /// 获取最近将要开始的比赛 id
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<int[]> GetUpcomingGames(CancellationToken token = default);

    /// <summary>
    /// Get game by Id
    /// </summary>
    /// <param name="id">比赛Id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Game?> GetGameById(int id, CancellationToken token = default);

    /// <summary>
    /// Check if the game is exists by Id
    /// </summary>
    /// <param name="id">Game Id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> HasGameAsync(int id, CancellationToken token = default);

    /// <summary>
    /// Create a new game
    /// </summary>
    /// <param name="game">比赛对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Game?> CreateGame(Game game, CancellationToken token = default);

    /// <summary>
    /// Update game
    /// </summary>
    /// <param name="game"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task UpdateGame(Game game, CancellationToken token = default);

    /// <summary>
    /// 获取队伍Token
    /// </summary>
    /// <param name="game">比赛对象</param>
    /// <param name="team">参赛队伍对象</param>
    /// <returns></returns>
    public string GetToken(Game game, Team team);

    /// <summary>
    /// 删除比赛
    /// </summary>
    /// <param name="game">比赛对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<TaskStatus> DeleteGame(Game game, CancellationToken token = default);

    /// <summary>
    /// 删除比赛的全部 WriteUp
    /// </summary>
    /// <param name="game">比赛对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task DeleteAllWriteUps(Game game, CancellationToken token = default);

    #region RecentGames

    /// <summary>
    /// 生成近期的比赛基本信息
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<BasicGameInfoModel[]> GenRecentGames(CancellationToken token = default);

    /// <summary>
    /// 获取近期的比赛基本信息
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<DataWithModifiedTime<BasicGameInfoModel[]>> GetRecentGames(CancellationToken token = default);

    #endregion

    #region Scoreboard

    /// <summary>
    /// Generate scoreboard for a game
    /// </summary>
    /// <param name="game">game</param>
    /// <param name="token"></param>
    public Task<ScoreboardModel> GenScoreboard(Game game, CancellationToken token = default);

    /// <summary>
    /// Get scoreboard by game
    /// </summary>
    /// <param name="game">Game</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<ScoreboardModel> GetScoreboard(Game game, CancellationToken token = default);

    /// <summary>
    /// Try to get scoreboard by game id, return null if not exists
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<ScoreboardModel?> TryGetScoreboard(int gameId, CancellationToken token = default);

    /// <summary>
    /// Get if the game is closed
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> IsGameClosed(int gameId, CancellationToken token = default);

    /// <summary>
    /// Get scoreboard with team members
    /// </summary>
    /// <param name="game">Game</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<ScoreboardModel> GetScoreboardWithMembers(Game game, CancellationToken token = default);

    #endregion
}
