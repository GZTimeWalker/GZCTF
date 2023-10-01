using GZCTF.Models.Request.Edit;

namespace GZCTF.Repositories.Interface;

public interface IGameChallengeRepository : IRepository
{
    /// <summary>
    /// 创建题目对象
    /// </summary>
    /// <param name="game">比赛对象</param>
    /// <param name="gameChallenge">题目对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameChallenge> CreateChallenge(Game game, GameChallenge gameChallenge, CancellationToken token = default);

    /// <summary>
    /// 移除题目对象
    /// </summary>
    /// <param name="gameChallenge">题目对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task RemoveChallenge(GameChallenge gameChallenge, CancellationToken token = default);

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
    public Task<GameChallenge?> GetChallenge(int gameId, int id, bool withFlag = false, CancellationToken token = default);

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
    /// <param name="gameChallenge">比赛题目对象</param>
    /// <param name="model">Flag 信息</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task AddFlags(GameChallenge gameChallenge, FlagCreateModel[] model, CancellationToken token = default);

    /// <summary>
    /// 确保此题目 Instance 对象已创建
    /// </summary>
    /// <param name="gameChallenge"></param>
    /// <param name="game"></param>
    /// <param name="token"></param>
    /// <returns>是否有实例更新</returns>
    public Task<bool> EnsureInstances(GameChallenge gameChallenge, Game game, CancellationToken token = default);

    /// <summary>
    /// 更新附件
    /// </summary>
    /// <param name="gameChallenge">比赛题目对象</param>
    /// <param name="model">附件信息</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task UpdateAttachment(GameChallenge gameChallenge, AttachmentCreateModel model, CancellationToken token = default);

    /// <summary>
    /// 删除 Flag，确保 Flags 字段已加载
    /// </summary>
    /// <param name="gameChallenge">比赛题目对象</param>
    /// <param name="flagId">flag ID</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<TaskStatus> RemoveFlag(GameChallenge gameChallenge, int flagId, CancellationToken token = default);

    /// <summary>
    /// 验证静态 Flag（可能多个答案）
    /// </summary>
    /// <param name="gameChallenge">题目对象</param>
    /// <param name="flag">Flag 字符串</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> VerifyStaticAnswer(GameChallenge gameChallenge, string flag, CancellationToken token = default);
}