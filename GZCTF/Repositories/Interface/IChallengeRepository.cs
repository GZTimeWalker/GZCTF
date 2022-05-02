using CTFServer.Models;
using CTFServer.Models.Request.Edit;
using CTFServer.Models.Request.Game;

namespace CTFServer.Repositories.Interface;

public interface IChallengeRepository : IRepository
{
    /// <summary>
    /// 创建题目对象
    /// </summary>
    /// <param name="game">比赛对象</param>
    /// <param name="challenge">题目对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Challenge> CreateChallenge(Game game, Challenge challenge, CancellationToken token = default);

    /// <summary>
    /// 移除题目对象
    /// </summary>
    /// <param name="challenge">题目对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task RemoveChallenge(Challenge challenge, CancellationToken token = default);

    /// <summary>
    /// 获取全部题目
    /// </summary>
    /// <param name="gameId">比赛Id</param>
    /// <param name="count">数量</param>
    /// <param name="skip">跳过</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Challenge[]> GetChallenges(int gameId, int count = 100, int skip = 0, CancellationToken token = default);

    /// <summary>
    /// 获取题目
    /// </summary>
    /// <param name="gameId">比赛Id</param>
    /// <param name="id">题目Id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Challenge?> GetChallenge(int gameId, int id, CancellationToken token = default);

    /// <summary>
    /// 添加 Flag
    /// </summary>
    /// <param name="challenge">比赛题目对象</param>
    /// <param name="model">Flag 信息</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<int> AddFlag(Challenge challenge, FlagInfoModel model, CancellationToken token = default);

    /// <summary>
    /// 删除 Flag
    /// </summary>
    /// <param name="challenge">比赛题目对象</param>
    /// <param name="flagId">flag ID</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<TaskStatus> RemoveFlag(Challenge challenge, int flagId, CancellationToken token = default);

    /// <summary>
    /// 验证静态 Flag（可能多个答案）
    /// </summary>
    /// <param name="challenge">题目对象</param>
    /// <param name="flag">Flag 字符串</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> VerifyStaticAnswer(Challenge challenge, string flag, CancellationToken token = default);

    /// <summary>
    /// 启用一个题目
    /// </summary>
    /// <param name="challenge">题目对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task EnableChallenge(Challenge challenge, CancellationToken token);
}
