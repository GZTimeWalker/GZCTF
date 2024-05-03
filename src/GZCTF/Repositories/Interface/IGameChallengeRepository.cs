using GZCTF.Models.Request.Edit;

namespace GZCTF.Repositories.Interface;

public interface IGameChallengeRepository : IRepository
{
    /// <summary>
    /// 创建题目对象
    /// </summary>
    /// <param name="game">比赛对象</param>
    /// <param name="challenge">题目对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameChallenge> CreateChallenge(Game game, GameChallenge challenge, CancellationToken token = default);

    /// <summary>
    /// 移除题目对象
    /// </summary>
    /// <param name="challenge">题目对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task RemoveChallenge(GameChallenge challenge, CancellationToken token = default);

    /// <summary>
    /// 获取全部题目
    /// </summary>
    /// <param name="gameId">比赛Id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameChallenge[]> GetChallenges(int gameId, CancellationToken token = default);

    /// <summary>
    /// 获取题目
    /// </summary>
    /// <param name="gameId">比赛Id</param>
    /// <param name="id">题目Id</param>
    /// <param name="withFlag">是否加载Flag</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameChallenge?> GetChallenge(int gameId, int id, bool withFlag = false,
        CancellationToken token = default);

    /// <summary>
    /// 获取全部需要捕获流量的题目
    /// </summary>
    /// <param name="gameId">比赛Id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameChallenge[]> GetChallengesWithTrafficCapturing(int gameId, CancellationToken token = default);

    /// <summary>
    /// 添加 Flag
    /// </summary>
    /// <param name="challenge">比赛题目对象</param>
    /// <param name="model">Flag 信息</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task AddFlags(GameChallenge challenge, FlagCreateModel[] model, CancellationToken token = default);

    /// <summary>
    /// 确保此题目 Instance 对象已创建
    /// </summary>
    /// <param name="challenge"></param>
    /// <param name="game"></param>
    /// <param name="token"></param>
    /// <returns>是否有实例更新</returns>
    public Task<bool> EnsureInstances(GameChallenge challenge, Game game, CancellationToken token = default);

    /// <summary>
    /// 更新附件
    /// </summary>
    /// <param name="challenge">比赛题目对象</param>
    /// <param name="model">附件信息</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task UpdateAttachment(GameChallenge challenge, AttachmentCreateModel model,
        CancellationToken token = default);

    /// <summary>
    /// 删除 Flag，确保 Flags 字段已加载
    /// </summary>
    /// <param name="challenge">比赛题目对象</param>
    /// <param name="flagId">flag ID</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<TaskStatus> RemoveFlag(GameChallenge challenge, int flagId, CancellationToken token = default);

    /// <summary>
    /// 重新计算解题数量
    /// </summary>
    /// <param name="game">比赛 id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> RecalculateAcceptedCount(Game game, CancellationToken token = default);
}