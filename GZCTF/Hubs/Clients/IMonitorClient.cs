namespace CTFServer.Hubs.Clients;

public interface IMonitorClient
{
    /// <summary>
    /// 接收到比赛事件信息
    /// </summary>
    public Task ReceivedGameEvent(GameEvent gameEvent);
}