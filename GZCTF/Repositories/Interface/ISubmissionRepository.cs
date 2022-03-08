using CTFServer.Models;
using CTFServer.Models.Request;
using CTFServer.Utils;

namespace CTFServer.Repositories.Interface;

public interface ISubmissionRepository
{
    /// <summary>
    /// 添加提交记录
    /// </summary>
    /// <param name="sub">用户提交</param>
    /// <param name="token">操作取消token</param>
    /// <returns></returns>
    public Task AddSubmission(Submission sub, CancellationToken token);

    /// <summary>
    /// 根据题目Id获取提交记录
    /// </summary>
    /// <param name="challengesId">题目Id</param>
    /// <param name="userId">用户Id</param>
    /// <param name="skip">跳过数量</param>
    /// <param name="count">获取数量</param>
    /// <param name="token">操作取消token</param>
    /// <returns></returns>
    public Task<List<Submission>> GetSubmissions(CancellationToken token, int skip = 0, int count = 50, int challengesId = 0, string userId = "All");

    /// <summary>
    /// 获取用户时间线
    /// </summary>
    /// <param name="user">包括了Submission的用户对象</param>
    /// <param name="token">操作取消Token</param>
    /// <returns></returns>
    public List<TimeLineModel> GetTimeLine(UserInfo user, CancellationToken token);

    /// <summary>
    /// 是否已经提交
    /// </summary>
    /// <param name="challengesId">题目Id</param>
    /// <param name="userId">用户Id</param>
    /// <param name="token">操作取消Token</param>
    /// <returns></returns>
    public Task<bool> HasSubmitted(int challengesId, string userId, CancellationToken token);
    
    /// <summary>
    /// 获取已解出的题目列表
    /// </summary>
    /// <param name="userId">用户Id</param>
    /// <param name="token">操作取消Token</param>
    /// <returns></returns>
    public Task<HashSet<int>> GetSolvedChallengess(string userId, CancellationToken token);
}
