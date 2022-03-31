using CTFServer.Models;

namespace CTFServer.Repositories.Interface;

public interface ISubmissionRepository
{
    /// <summary>
    /// 获取提交，按时间降序
    /// </summary>
    /// <param name="count">数量</param>
    /// <param name="skip">跳过数量</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<List<Submission>> GetSubmissions(int count = 100, int skip = 0, CancellationToken token = default);

    /// <summary>
    /// 获取比赛的提交，按时间降序
    /// </summary>
    /// <param name="game">比赛对象</param>
    /// <param name="count">数量</param>
    /// <param name="skip">跳过数量</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<List<Submission>> GetSubmissions(Game game, int count = 100, int skip = 0, CancellationToken token = default);

    /// <summary>
    /// 获取题目的提交，按时间降序
    /// </summary>
    /// <param name="challenge">题目对象</param>
    /// <param name="count">数量</param>
    /// <param name="skip">跳过数量</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<List<Submission>> GetSubmissions(Challenge challenge, int count = 100, int skip = 0, CancellationToken token = default);

    /// <summary>
    /// 获取队伍的提交，按时间降序
    /// </summary>
    /// <param name="team">队伍参赛对象</param>
    /// <param name="count">数量</param>
    /// <param name="skip">跳过数量</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<List<Submission>> GetSubmissions(Participation team, int count = 100, int skip = 0, CancellationToken token = default);
    
    /// <summary>
    /// 添加提交
    /// </summary>
    /// <param name="submission">提交对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Submission> AddSubmission(Submission submission, CancellationToken token = default);
}
