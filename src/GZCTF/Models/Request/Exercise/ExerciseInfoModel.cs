namespace GZCTF.Models.Request.Exercise;

/// <summary>
/// Basic exercise information
/// </summary>
public class ExerciseInfoModel
{
    /// <summary>
    /// Exercise ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Exercise title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Difficulty of the exercise, used for tags, sorting, etc.
    /// </summary>
    public Difficulty Difficulty { get; set; }

    /// <summary>
    /// Exercise category
    /// </summary>
    public ChallengeCategory Category { get; set; }

    /// <summary>
    /// Additional tags for the exercise
    /// </summary>
    public List<string>? Tags { get; set; } = new();

    /// <summary>
    /// Exercise points
    /// </summary>
    public int Credit { get; set; }

    /// <summary>
    /// Number of people who solved the exercise
    /// </summary>
    public int AcceptedCount { get; set; }

    /// <summary>
    /// Number of submissions
    /// </summary>
    public int SubmissionCount { get; set; }
}
