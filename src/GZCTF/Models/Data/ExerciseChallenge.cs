namespace GZCTF.Models.Data;

public class ExerciseChallenge : Challenge
{
    /// <summary>
    /// Credits for the exercise challenge
    /// </summary>
    public bool Credit { get; set; }

    /// <summary>
    /// Difficulty of the exercise challenge, used for tags, sorting, etc.
    /// </summary>
    public Difficulty Difficulty { get; set; }

    /// <summary>
    /// Additional tags for the exercise challenge
    /// </summary>
    public List<string>? Tags { get; set; } = [];

    #region Db Relationship

    /// <summary>
    /// Dependent exercise challenges
    /// </summary>
    public List<ExerciseChallenge> Dependencies { get; set; } = [];

    #endregion
}
