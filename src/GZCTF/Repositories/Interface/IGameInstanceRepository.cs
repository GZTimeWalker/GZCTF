using GZCTF.Models.Internal;

namespace GZCTF.Repositories.Interface;

public interface IGameInstanceRepository : IRepository
{
    /// <summary>
    /// 获取或创建队伍的题目实例
    /// </summary>
    /// <param name="team">队伍</param>
    /// <param name="challengeId">题目Id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameInstance?> GetInstance(Participation team, int challengeId, CancellationToken token = default);

    /// <summary>
    /// 验证答案
    /// </summary>
    /// <param name="submission">当前提交</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<VerifyResult> VerifyAnswer(Submission submission, CancellationToken token = default);

    /// <summary>
    /// 检查抄袭行为
    /// </summary>
    /// <param name="submission">当前提交</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<CheatCheckInfo> CheckCheat(Submission submission, CancellationToken token = default);

    /// <summary>
    /// 创建容器实例
    /// </summary>
    /// <param name="team">队伍信息</param>
    /// <param name="game">比赛对象</param>
    /// <param name="user">用户对象</param>
    /// <param name="gameInstance">实例对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<TaskResult<Container>> CreateContainer(GameInstance gameInstance, Team team, UserInfo user,
        Game game, CancellationToken token = default);

    /// <summary>
    /// 销毁全部题目实例
    /// </summary>
    /// <param name="challenge">当前题目</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task DestroyAllInstances(GameChallenge challenge, CancellationToken token = default);
}
