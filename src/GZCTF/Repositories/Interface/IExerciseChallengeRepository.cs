using GZCTF.Models.Request.Edit;

namespace GZCTF.Repositories.Interface;

public interface IExerciseChallengeRepository : IRepository
{
    /// <summary>
    /// 创建练习题目
    /// </summary>
    /// <param name="exercise">练习题目</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<ExerciseChallenge> CreateExercise(ExerciseChallenge exercise, CancellationToken token = default);

    /// <summary>
    /// 移除练习题目
    /// </summary>
    /// <param name="exercise">练习题目</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task RemoveExercise(ExerciseChallenge exercise, CancellationToken token = default);

    /// <summary>
    /// 获取练习题目
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<ExerciseChallenge[]> GetExercises(CancellationToken token = default);

    /// <summary>
    /// 更新附件
    /// </summary>
    /// <param name="exercise">比赛题目对象</param>
    /// <param name="model">附件信息</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task UpdateAttachment(ExerciseChallenge exercise, AttachmentCreateModel model,
        CancellationToken token = default);
}
