using CTFServer.Models;
using CTFServer.Models.Request.Edit;

namespace CTFServer.Repositories.Interface;

public interface IChallengeRepository
{
    /// <summary>
    /// 创建题目对象
    /// </summary>
    /// <param name="game">比赛对象</param>
    /// <param name="model">题目信息</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Challenge> CreateChallenge(Game game, ChallengeInfoModel model, CancellationToken token = default);

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

    /// <summary>
    /// 随机获取未占用的题目 Flag 上下文
    /// </summary>
    /// <param name="challenge">题目对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<FlagContext?> GetChallengeRandomFlag(Challenge challenge, CancellationToken token = default);

    /// <summary>
    /// 验证静态 Flag（可能多个答案）
    /// </summary>
    /// <param name="challenge">题目对象</param>
    /// <param name="flag">Flag 字符串</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> VerifyStaticAnswer(Challenge challenge, string flag, CancellationToken token = default);
}
