using CTFServer.Models;

namespace CTFServer.Repositories.Interface;

public interface IEventRepository
{
    /// <summary>
    /// 添加一个事件
    /// </summary>
    /// <param name="game">比赛对象</param>
    /// <param name="event">事件</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Event> AddEvent(Game game, Event @event, CancellationToken token = default);

    /// <summary>
    /// 获取全部事件
    /// </summary>
    /// <param name="game">比赛对象</param>
    /// <param name="count">数量</param>
    /// <param name="skip">跳过数量</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<List<Event>> GetEvents(Game game, int count = 10, int skip = 0, CancellationToken token = default);
}
