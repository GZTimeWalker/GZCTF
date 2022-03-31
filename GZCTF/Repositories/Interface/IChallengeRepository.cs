using CTFServer.Models;
using CTFServer.Models.Request.Edit;

namespace CTFServer.Repositories.Interface;

public interface IChallengeRepository
{
    /// <summary>
    /// 创建题目和排名对象
    /// </summary>
    /// <param name="game">比赛对象</param>
    /// <param name="challenge">题目信息</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Challenge> CreateChallengeWithRank(Game game, ChallengeInfoModel challenge, CancellationToken token = default);

    /// <summary>
    /// 获取全部题目
    /// </summary>
    /// <param name="count">数量</param>
    /// <param name="skip">跳过</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<List<Challenge>> GetChallenges(int count = 100, int skip = 0, CancellationToken token = default);
    
    /// <summary>
    /// 获取比赛全部题目
    /// </summary>
    /// <param name="game">比赛</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<List<Challenge>> GetChallenges(Game game, CancellationToken token = default);
}
