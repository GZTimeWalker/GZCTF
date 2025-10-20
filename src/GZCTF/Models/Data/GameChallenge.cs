using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GZCTF.Models.Request.Edit;

namespace GZCTF.Models.Data;

public class GameChallenge : Challenge
{
    /// <summary>
    /// Whether to record traffic
    /// </summary>
    public bool EnableTrafficCapture { get; set; }

    /// <summary>
    /// Whether to disable blood bonus
    /// </summary>
    public bool DisableBloodBonus { get; set; }

    /// <summary>
    /// Initial score
    /// </summary>
    [Required]
    public int OriginalScore { get; set; } = 1000;

    /// <summary>
    /// Minimum score rate
    /// </summary>
    [Required]
    [Range(0, 1)]
    public double MinScoreRate { get; set; } = 0.25;

    /// <summary>
    /// Difficulty coefficient
    /// </summary>
    [Required]
    public double Difficulty { get; set; } = 5;

    /// <summary>
    /// Current score of the challenge
    /// </summary>
    [NotMapped]
    public int CurrentScore
    {
        get
        {
            var solveCount = FirstSolves?.Count ?? 0;

            if (solveCount <= 1)
                return OriginalScore;

            return (int)Math.Floor(
                OriginalScore * (MinScoreRate +
                                 (1.0 - MinScoreRate) * Math.Exp((1 - solveCount) / Difficulty)
                ));
        }
    }

    internal void Update(ChallengeUpdateModel model)
    {
        Title = model.Title ?? Title;
        Content = model.Content ?? Content;
        Category = model.Category ?? Category;
        Hints = model.Hints ?? Hints;
        CPUCount = model.CPUCount ?? CPUCount;
        MemoryLimit = model.MemoryLimit ?? MemoryLimit;
        StorageLimit = model.StorageLimit ?? StorageLimit;
        ContainerImage = model.ContainerImage?.Trim() ?? ContainerImage;
        ContainerExposePort = model.ContainerExposePort ?? ContainerExposePort;
        OriginalScore = model.OriginalScore ?? OriginalScore;
        MinScoreRate = model.MinScoreRate ?? MinScoreRate;
        Difficulty = model.Difficulty ?? Difficulty;
        FileName = model.FileName ?? FileName;
        DisableBloodBonus = model.DisableBloodBonus ?? DisableBloodBonus;
        SubmissionLimit = model.SubmissionLimit ?? SubmissionLimit;

        // isEnabled should be updated alone
        IsEnabled = model.IsEnabled ?? IsEnabled;

        // only set DeadlineUtc to null when pass DateTimeOffset.MinValue (but not null)
        if (model.DeadlineUtc is { } time)
            DeadlineUtc = time.ToUnixTimeSeconds() == 0 ? null : time;

        // only set FlagTemplate to null when pass an empty string (but not null)
        if (model.FlagTemplate is { } template)
            FlagTemplate = string.IsNullOrWhiteSpace(template) ? null : template;

        // Container only
        EnableTrafficCapture = Type.IsContainer() && (model.EnableTrafficCapture ?? EnableTrafficCapture);
    }

    #region Db Relationship

    /// <summary>
    /// Submissions
    /// </summary>
    public List<Submission> Submissions { get; set; } = [];

    /// <summary>
    /// Challenge instances
    /// </summary>
    public List<GameInstance> Instances { get; set; } = [];

    /// <summary>
    /// Teams that activated the challenge
    /// </summary>
    public HashSet<Participation> Teams { get; set; } = [];

    /// <summary>
    /// Configurations for divisions
    /// </summary>
    public HashSet<DivisionChallengeConfig> DivisionConfigs { get; set; } = [];

    /// <summary>
    /// First solves recorded for this challenge.
    /// </summary>
    public List<FirstSolve>? FirstSolves { get; set; } = [];

    /// <summary>
    /// Game ID
    /// </summary>
    public int GameId { get; set; }

    /// <summary>
    /// Game object
    /// </summary>
    public Game Game { get; set; } = null!;

    #endregion Db Relationship
}
