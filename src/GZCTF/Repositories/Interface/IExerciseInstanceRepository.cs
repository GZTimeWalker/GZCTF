namespace GZCTF.Repositories.Interface;

public interface IExerciseInstanceRepository : IRepository
{
    /// <summary>
    /// 获取练习题实例
    /// </summary>
    /// <param name="user"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<ExerciseInstance[]> GetExerciseInstances(UserInfo user, CancellationToken token = default);

    /// <summary>
    /// 验证答案
    /// </summary>
    /// <param name="answer">当前提交</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<VerifyResult> VerifyAnswer(string answer, CancellationToken token = default);

    /// <summary>
    /// 创建容器实例
    /// </summary>
    /// <param name="instance">实例对象</param>
    /// <param name="containerLimit">容器数量限制</param>
    /// <param name="user">用户对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<TaskResult<Container>> CreateContainer(ExerciseInstance instance, UserInfo user, int containerLimit = 3, CancellationToken token = default);

    /// <summary>
    /// 销毁容器实例
    /// </summary>
    /// <param name="container">容器实例对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> DestroyContainer(Container container, CancellationToken token = default);

    /// <summary>
    /// 容器延期
    /// </summary>
    /// <param name="container">容器实例对象</param>
    /// <param name="time">延长时间</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task ProlongContainer(Container container, TimeSpan time, CancellationToken token = default);
}
