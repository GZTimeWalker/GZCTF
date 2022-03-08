using CTFServer.Models;
using CTFServer.Models.Request;

namespace CTFServer.Repositories.Interface;

public interface IRankRepository
{
    /// <summary>
    /// 获取包括用户和提交信息的排名
    /// </summary>
    /// <param name="skip">跳过数量</param>
    /// <param name="count">获取数量</param>
    /// <param name="token">操作取消token</param>
    /// <returns></returns>
    public Task<List<RankMessageModel>> GetRank(CancellationToken token, int skip = 0, int count = 100);

    /// <summary>
    /// 更新排名对象
    /// </summary>
    /// <param name="rank">排名对象</param>
    /// <param name="score">分数</param>
    /// <param name="token">操作取消token</param>
    /// <returns></returns>
    public Task UpdateRank(Rank rank, int score, CancellationToken token);

    /// <summary>
    /// 检查用户提交记录，并进行分数校对
    /// </summary>
    /// <param name="token">操作取消token</param>
    /// <returns></returns>
    public Task CheckRankScore(CancellationToken token);
}
