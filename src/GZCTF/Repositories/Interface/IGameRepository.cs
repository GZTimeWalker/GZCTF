using GZCTF.Models.Request.Game;

namespace GZCTF.Repositories.Interface;

public interface IGameRepository : IRepository
{
    /// <summary>
    /// Get basic info of games with pagination (with caching)
    /// </summary>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<ArrayResponse<BasicGameInfoModel>> GetGameInfo(int count = 10, int skip = 0,
        CancellationToken token = default);

    /// <summary>
    /// Fetch basic info of games with pagination
    /// </summary>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<BasicGameInfoModel[]> FetchGameList(int count, int skip, CancellationToken token);

    /// <summary>
    /// Get detailed game info by ID, without team-specific data
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<DetailedGameInfoModel?> GetDetailedGameInfo(int gameId, CancellationToken token = default);

    /// <summary>
    /// Get games with pagination for admins
    /// </summary>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Game[]> GetGames(int count = 10, int skip = 0, CancellationToken token = default);

    /// <summary>
    /// Get upcoming games' Ids
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<int[]> GetUpcomingGames(CancellationToken token = default);

    /// <summary>
    /// Get game by ID
    /// </summary>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Game?> GetGameById(int id, CancellationToken token = default);

    /// <summary>
    /// Get check info for joining a game
    /// </summary>
    /// <param name="game"></param>
    /// <param name="user"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameJoinCheckInfoModel> GetCheckInfo(Game game, UserInfo user, CancellationToken token = default);

    /// <summary>
    /// Load divisions for a game
    /// </summary>
    /// <param name="game"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task LoadDivisions(Game game, CancellationToken token = default);

    /// <summary>
    /// Check if the game exists by ID
    /// </summary>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> HasGameAsync(int id, CancellationToken token = default);

    /// <summary>
    /// Create a new game
    /// </summary>
    /// <param name="game"></param>
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
    /// Get token for a team in a game
    /// </summary>
    /// <param name="game">The game</param>
    /// <param name="team">The team</param>
    /// <returns></returns>
    public string GetToken(Game game, Team team);

    /// <summary>
    /// Delete a game and all its related data
    /// </summary>
    /// <param name="game"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<TaskStatus> DeleteGame(Game game, CancellationToken token = default);

    /// <summary>
    /// Delete all write-ups of a game
    /// </summary>
    /// <param name="game"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task DeleteAllWriteUps(Game game, CancellationToken token = default);

    #region RecentGames

    /// <summary>
    /// Generate recent games' basic information
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<BasicGameInfoModel[]> GenRecentGames(CancellationToken token = default);

    /// <summary>
    /// Get recent games' basic information (with caching)
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
