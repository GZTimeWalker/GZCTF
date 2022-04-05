using CTFServer.Models;
using CTFServer.Utils;

namespace CTFServer.Repositories.Interface;

public interface IInstanceRepository : IRepository
{
    /// <summary>
    /// 创建队伍的题目实例
    /// </summary>
    /// <param name="team">队伍</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Participation> CreateInstances(Participation team, CancellationToken token = default);

    /// <summary>
    /// 验证答案
    /// </summary>
    /// <param name="instance">实例</param>
    /// <param name="flag">Flag 字符串</param>
    /// <returns></returns>
    public AnswerResult VerifyAnswer(Instance instance, string flag);

    /// <summary>
    /// 获取题目实例
    /// </summary>
    /// <param name="team">参与队伍</param>
    /// <param name="challenge">题目对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Instance?> GetInstance(Participation team, Challenge challenge, CancellationToken token = default);

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
