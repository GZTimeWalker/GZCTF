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
    /// 练习标签
    /// </summary>
    public ChallengeTag Tag { get; set; }

    /// <summary>
    /// 练习积分
    /// </summary>
    public int Credit { get; set; }

    /// <summary>
    /// 解出队伍数量
    /// </summary>
    public int SolvedCount { get; set; }

    /// <summary>
    /// 提交数量
    /// </summary>
    public int SubmissionCount { get; set; }
}
