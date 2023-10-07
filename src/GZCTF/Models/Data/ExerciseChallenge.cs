namespace GZCTF.Models.Data;

public class ExerciseChallenge : Challenge
{
    /// <summary>
    /// 题目的积分
    /// </summary>
    public bool Credit { get; set; }

    #region Db Relationship

    /// <summary>
    /// 依赖的题目
    /// </summary>
    public List<ExerciseChallenge> Dependencies { get; set; } = new();

    #endregion
}
