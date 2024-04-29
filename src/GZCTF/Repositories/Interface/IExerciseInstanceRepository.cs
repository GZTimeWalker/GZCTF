namespace GZCTF.Repositories.Interface;

public interface IExerciseInstanceRepository : IRepository
{
    /// <summary>
    /// 获取用户的题目实例
    /// </summary>
    /// <description>
    /// 获取用户的题目实例，创建实例的工作在检查正确性及依赖关系后自动完成
    /// 可以认为，只有拥有题目实例的用户才是有权限访问题目的
    /// </description>
    /// <param name="user">用户</param>
    /// <param name="exerciseId">题目Id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<ExerciseInstance?> GetInstance(UserInfo user, int exerciseId, CancellationToken token = default);

    /// <summary>
    /// 获取练习题实例
    /// </summary>
    /// <param name="user"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<ExerciseInstance[]> GetExerciseInstances(UserInfo user, CancellationToken token = default);

    /// <summary>
    /// 验证答案并解锁题目
    /// </summary>
    /// <param name="user">当前用户</param>
    /// <param name="instance">当前实例</param>
    /// <param name="answer">当前提交</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<AnswerResult> VerifyAnswer(UserInfo user, ExerciseInstance instance, string answer,
        CancellationToken token = default);

    /// <summary>
    /// 创建容器实例
    /// </summary>
    /// <param name="instance">实例对象</param>
    /// <param name="user">用户对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<TaskResult<Container>> CreateContainer(ExerciseInstance instance, UserInfo user,
        CancellationToken token = default);
}
