using CTFServer.Models.Request.Edit;

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
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Challenge[]> GetChallenges(int gameId, CancellationToken token = default);

    /// <summary>
    /// 获取题目
    /// </summary>
    /// <param name="gameId">比赛Id</param>
    /// <param name="id">题目Id</param>
    /// <param name="withFlag">是否加载Flag</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Challenge?> GetChallenge(int gameId, int id, bool withFlag = false, CancellationToken token = default);

    /// <summary>
    /// 添加 Flag
    /// </summary>
    /// <param name="challenge">比赛题目对象</param>
    /// <param name="model">Flag 信息</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task AddFlags(Challenge challenge, FlagCreateModel[] model, CancellationToken token = default);

    /// <summary>
    /// 确保此题目 Instance 对象已创建
    /// </summary>
    /// <param name="challenge"></param>
    /// <param name="game"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task EnsureInstances(Challenge challenge, Game game, CancellationToken token = default);

    /// <summary>
    /// 更新附件
    /// </summary>
    /// <param name="challenge">比赛题目对象</param>
    /// <param name="model">附件信息</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task UpdateAttachment(Challenge challenge, AttachmentCreateModel model, CancellationToken token = default);

    /// <summary>
    /// 删除 Flag，确保 Flags 字段已加载
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
}