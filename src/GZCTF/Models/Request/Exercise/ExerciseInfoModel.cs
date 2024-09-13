namespace GZCTF.Models.Request.Exercise;

/// <summary>
/// 练习基本信息
/// </summary>
public class ExerciseInfoModel
{
    /// <summary>
    /// 练习 Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 练习名称
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 练习题目的难度，用作标签、排序等
    /// </summary>
    public Difficulty Difficulty { get; set; }

    /// <summary>
    /// 练习标签
    /// </summary>
    public ChallengeCategory Category { get; set; }

    /// <summary>
    /// 练习附加标签
    /// </summary>
    public List<string>? Tags { get; set; } = new();

    /// <summary>
    /// 练习积分
    /// </summary>
    public int Credit { get; set; }

    /// <summary>
    /// 解决题目人数
    /// </summary>
    public int AcceptedCount { get; set; }

    /// <summary>
    /// 提交答案的数量
    /// </summary>
    public int SubmissionCount { get; set; }
}
