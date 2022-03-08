using CTFServer.Models;
using CTFServer.Models.Request;
using CTFServer.Utils;

namespace CTFServer.Repositories.Interface;

public interface IChallengeRepository
{
    /// <summary>
    /// 添加一个题目对象
    /// </summary>
    /// <param name="newChallenge">待添加的题目</param>
    /// <param name="token">操作取消token</param>
    /// <returns></returns>
    public Task<Challenge> AddChallenge(ChallengeBase newChallenge, CancellationToken token);

    /// <summary>
    /// 获取用户题目数据
    /// </summary>
    /// <param name="id">题目Id</param>
    /// <param name="accessLevel">用户访问权限</param>
    /// <param name="token">操作取消token</param>
    /// <returns>用户题目</returns>
    public Task<UserChallengesModel?> GetUserChallenges(int id, int accessLevel, CancellationToken token);

    /// <summary>
    /// 更新一个题目对象
    /// </summary>
    /// <param name="id">题目Id</param>
    /// <param name="newChallenge">更新的题目数据</param>
    /// <param name="token">操作取消token</param>
    /// <returns></returns>
    public Task<Challenge?> UpdateChallenges(int id, ChallengeBase newChallenge, CancellationToken token);

    /// <summary>
    /// 删除题目
    /// </summary>
    /// <param name="id">题目Id</param>
    /// <param name="token">操作取消token</param>
    /// <returns></returns>
    public Task<(bool result, string title)> DeleteChallenges(int id, CancellationToken token);

    /// <summary>
    /// 验证答案
    /// </summary>
    /// <param name="id">题目Id</param>
    /// <param name="answer">答案</param>
    /// <param name="user">用户对象</param>
    /// <param name="hasSolved">是否已经有过成功的提交</param>
    /// <param name="token">操作取消token</param>
    /// <returns></returns>
    public Task<VerifyResult> VerifyAnswer(int id, string? answer, UserInfo user, bool hasSolved, CancellationToken token);

    /// <summary>
    /// 获取可访问题目列表
    /// </summary>
    /// <param name="accessLevel">用户访问权限</param>
    /// <param name="token">操作取消token</param>
    /// <returns></returns>
    public Task<List<ChallengesItem>> GetAccessibleChallengess(int accessLevel, CancellationToken token);

    /// <summary>
    /// 获取最高访问权限
    /// </summary>
    /// <returns></returns>
    public int GetMaxAccessLevel();
}
