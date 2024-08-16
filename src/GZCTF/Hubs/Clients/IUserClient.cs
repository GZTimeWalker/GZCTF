namespace GZCTF.Hubs.Clients;

public interface IUserClient
{
    /// <summary>
    /// 接收到比赛通知信息
    /// </summary>
    public Task ReceivedGameNotice(GameNotice notice);
}
