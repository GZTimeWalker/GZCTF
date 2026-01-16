namespace GZCTF.Repositories.Interface;

public interface IGameEventRepository : IRepository
{
    /// <summary>
    /// Add a new event
    /// </summary>
    /// <param name="event"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameEvent> AddEvent(GameEvent @event, CancellationToken token = default);

    /// <summary>
    /// Get events for a specific game with pagination
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="hideContainer">Set to true to hide container events</param>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="search">Search query to filter events</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameEvent[]> GetEvents(int gameId, bool hideContainer = false, int count = 50, int skip = 0,
        string? search = null, CancellationToken token = default);

    /// <summary>
    /// Check if a challenge has been opened by a team
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="teamId"></param>
    /// <param name="challengeId"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> IsChallengeOpened(int gameId, int teamId, int challengeId, CancellationToken token = default);
}
