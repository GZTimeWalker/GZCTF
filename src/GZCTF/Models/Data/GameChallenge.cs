using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GZCTF.Models.Request.Edit;

namespace GZCTF.Models.Data;

public class GameChallenge : Challenge
{
    /// <summary>
    /// 是否需要记录访问流量
    /// </summary>
    public bool EnableTrafficCapture { get; set; }

    /// <summary>
    /// 初始分数
    /// </summary>
    [Required]
    public int OriginalScore { get; set; } = 1000;

    /// <summary>
    /// 最低分数比例
    /// </summary>
    [Required]
    [Range(0, 1)]
    public double MinScoreRate { get; set; } = 0.25;

    /// <summary>
    /// 难度系数
    /// </summary>
    [Required]
    public double Difficulty { get; set; } = 5;

    /// <summary>
    /// 当前题目分值
    /// </summary>
    [NotMapped]
    public int CurrentScore =>
        AcceptedCount <= 1
            ? OriginalScore
            : (int)Math.Floor(
                OriginalScore * (MinScoreRate +
                                 (1.0 - MinScoreRate) * Math.Exp((1 - AcceptedCount) / Difficulty)
                ));

    internal void Update(ChallengeUpdateModel model)
    {
        Title = model.Title ?? Title;
        Content = model.Content ?? Content;
        Tag = model.Tag ?? Tag;
        Hints = model.Hints ?? Hints;
        IsEnabled = model.IsEnabled ?? IsEnabled;
        CPUCount = model.CPUCount ?? CPUCount;
        MemoryLimit = model.MemoryLimit ?? MemoryLimit;
        StorageLimit = model.StorageLimit ?? StorageLimit;
        ContainerImage = model.ContainerImage?.Trim() ?? ContainerImage;
        ContainerExposePort = model.ContainerExposePort ?? ContainerExposePort;
        OriginalScore = model.OriginalScore ?? OriginalScore;
        MinScoreRate = model.MinScoreRate ?? MinScoreRate;
        Difficulty = model.Difficulty ?? Difficulty;
        FileName = model.FileName ?? FileName;

        // only set FlagTemplate to null when it pass an empty string (but not null)
        FlagTemplate = model.FlagTemplate is null ? FlagTemplate :
            string.IsNullOrWhiteSpace(model.FlagTemplate) ? null : model.FlagTemplate;

        // Container only
        EnableTrafficCapture = Type.IsContainer() && (model.EnableTrafficCapture ?? EnableTrafficCapture);
    }

    #region Db Relationship

    /// <summary>
    /// 提交
    /// </summary>
    public List<Submission> Submissions { get; set; } = [];

    /// <summary>
    /// 赛题实例
    /// </summary>
    public List<GameInstance> Instances { get; set; } = [];

    /// <summary>
    /// 激活赛题的队伍
    /// </summary>
    public HashSet<Participation> Teams { get; set; } = [];

    /// <summary>
    /// 比赛 Id
    /// </summary>
    public int GameId { get; set; }

    /// <summary>
    /// 比赛对象
    /// </summary>
    public Game Game { get; set; } = default!;

    #endregion Db Relationship
}
