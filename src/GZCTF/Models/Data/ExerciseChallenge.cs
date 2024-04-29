namespace GZCTF.Models.Data;

public class ExerciseChallenge : Challenge
{
    /// <summary>
    /// 练习题目的积分
    /// </summary>
    public bool Credit { get; set; }

    /// <summary>
    /// 练习题目的难度，用作标签、排序等
    /// </summary>
    public Difficulty Difficulty { get; set; }

    /// <summary>
    /// 练习题目附加标签
    /// </summary>
    public List<string>? Tags { get; set; } = [];

    #region Db Relationship

    /// <summary>
    /// 依赖的练习题目
    /// </summary>
    public List<ExerciseChallenge> Dependencies { get; set; } = [];

    #endregion
}
