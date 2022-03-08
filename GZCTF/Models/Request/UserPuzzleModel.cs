namespace CTFServer.Models.Request;

/// <summary>
/// 用户端题目
/// </summary>
public class UserChallengesModel
{
    /// <summary>
    /// 题目标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 题目内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 客户端脚本
    /// </summary>
    public string? ClientJS {  get; set; }

    /// <summary>
    /// 已解出人数
    /// </summary>
    public int AcceptedCount { get; set; }

    /// <summary>
    /// 提交人数
    /// </summary>
    public int SubmissionCount { get; set; }
}
