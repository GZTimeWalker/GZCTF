using CTFServer.Models.Internal;
using CTFServer.Utils;

namespace CTFServer.Repositories.Interface;

public interface IInstanceRepository : IRepository
{
    /// <summary>
    /// 获取或创建队伍的题目实例
    /// </summary>
    /// <param name="team">队伍</param>
    /// <param name="challengeId">题目Id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Instance?> GetInstance(Participation team, int challengeId, CancellationToken token = default);

    /// <summary>
    /// 验证答案
    /// </summary>
    /// <param name="submission">当前提交</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<(SubmissionType, AnswerResult)> VerifyAnswer(Submission submission, CancellationToken token = default);

    /// <summary>
    /// 获取题目实例
    /// </summary>
    /// <param name="challenge">当前题目</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Instance[]> GetInstances(Challenge challenge, CancellationToken token = default);

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
    /// <param name="instance">实例对象</param>
    /// <param name="team">队伍信息</param>
    /// <param name="containerLimit">容器数量限制</param>
    /// <param name="userId">用户Id，用于记录</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<TaskResult<Container>> CreateContainer(Instance instance, Team team, string userId, int containerLimit = 3, CancellationToken token = default);

    /// <summary>
    /// 销毁容器实例
    /// </summary>
    /// <param name="container">容器实例对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> DestoryContainer(Container container, CancellationToken token = default);

    /// <summary>
    /// 容器延期
    /// </summary>
    /// <param name="container">容器实例对象</param>
    /// <param name="time">延长时间</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task ProlongContainer(Container container, TimeSpan time, CancellationToken token = default);
}