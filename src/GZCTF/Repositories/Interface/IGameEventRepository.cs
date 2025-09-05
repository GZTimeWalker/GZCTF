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
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameEvent[]> GetEvents(int gameId, bool hideContainer = false, int count = 50, int skip = 0,
        CancellationToken token = default);
}
