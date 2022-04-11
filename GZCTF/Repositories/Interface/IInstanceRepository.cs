using CTFServer.Models;
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
    public Task<AnswerResult> VerifyAnswer(Submission submission, CancellationToken token = default);

    /// <summary>
    /// 获取题目实例
    /// </summary>
    /// <param name="game">当前比赛</param>
    /// <param name="count">获取数量</param>
    /// <param name="skip">跳过数量</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Instance[]> GetInstances(Game game, int count = 30, int skip = 0, CancellationToken token = default);

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
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<TaskResult<Container>> CreateContainer(Instance instance, CancellationToken token = default);

    /// <summary>
    /// 销毁容器实例
    /// </summary>
    /// <param name="container">容器实例对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task DestoryContainer(Container container, CancellationToken token = default);

    /// <summary>
    /// 容器延期
    /// </summary>
    /// <param name="container">容器实例对象</param>
    /// <param name="time">延长时间</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task ProlongContainer(Container container, TimeSpan time, CancellationToken token = default);
}
