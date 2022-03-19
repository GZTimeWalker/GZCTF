using CTFServer.Models;

namespace CTFServer.Repositories.Interface;

public interface IGameRepository
{
    /// <summary>
    /// 获取指定数量的比赛对象
    /// </summary>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <returns></returns>
    public Task<List<Game>> GetGames(int count = 10, int skip = 0);
}
