
namespace CTFServer.Models.Request;

/// <summary>
/// 题目列表对象
/// </summary>
public class PuzzleListModel
{

    /// <summary>
    /// 可访问的题目
    /// </summary>
    public List<PuzzleItem> Accessible { get; set; } = new();
    /// <summary>
    /// 已经解决的题目序号列表
    /// </summary>
    public HashSet<int> Solved { get; set; } = new();
}

/// <summary>
/// 题目
/// </summary>
public class PuzzleItem
{
    /// <summary>
    /// 题目Id
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// 题目标题
    /// </summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// 解出的人数
    /// </summary>
    public int AcceptedCount { get; set; } = 0;

    /// <summary>
    /// 提交的人数
    /// </summary>
    public int SubmissionCount { get; set; } = 0;

    /// <summary>
    /// 当前分数
    /// </summary>
    public int Score { get; set; } = 0;
}