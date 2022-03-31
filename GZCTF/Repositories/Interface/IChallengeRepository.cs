using CTFServer.Models;

namespace CTFServer.Repositories.Interface;

public interface IChallengeRepository
{
    /// <summary>
    /// 创建题目
    /// </summary>
    /// <param name="challenge"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Challenge> CreateChallenge(Challenge challenge, CancellationToken token = default);
}
