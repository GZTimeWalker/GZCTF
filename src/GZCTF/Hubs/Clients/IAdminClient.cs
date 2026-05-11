using GZCTF.Models.Request.Admin;

namespace GZCTF.Hubs.Clients;

public interface IAdminClient
{
    /// <summary>
    /// 接收到广播日志信息
    /// </summary>
    public Task ReceivedLog(LogMessageModel log);

    /// <summary>
    /// Receive a honeypot hit notice for the admin live feed.
    /// </summary>
    public Task ReceivedHoneypotHit(HoneypotHitModel hit);

    /// <summary>
    /// Receive a flag-egress hit notice for the admin live feed.
    /// </summary>
    public Task ReceivedFlagEgress(FlagEgressHitModel hit);
}
