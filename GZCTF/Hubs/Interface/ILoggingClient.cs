using CTFServer.Models;

namespace CTFServer.Hubs.Interface;

public interface ILoggingClient
{
    /// <summary>
    /// 接收到广播日志信息
    /// </summary>
    public Task ReceivedLog(LogMessageModel log);
}
