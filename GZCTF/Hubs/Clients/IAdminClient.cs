using CTFServer.Models.Request.Admin;

namespace CTFServer.Hubs.Client;

public interface IAdminClient
{
    /// <summary>
    /// 接收到广播日志信息
    /// </summary>
    public Task ReceivedLog(LogMessageModel log);
}
