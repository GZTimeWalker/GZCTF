namespace GZCTF.Repositories.Interface;

public interface IGameEventRepository : IRepository
{
    /// <summary>
    /// 添加一个事件
    /// </summary>
    /// <param name="event">事件</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameEvent> AddEvent(GameEvent @event, CancellationToken token = default);

    /// <summary>
    /// 获取全部事件
    /// </summary>
    /// <param name="gameId">比赛Id</param>
    /// <param name="hideContainer">隐藏容器事件</param>
    /// <param name="count">数量</param>
    /// <param name="skip">跳过数量</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameEvent[]> GetEvents(int gameId, bool hideContainer = false, int count = 50, int skip = 0,
        CancellationToken token = default);
}
