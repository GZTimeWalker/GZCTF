using CTFServer.Models.Request.Edit;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CTFServer.Models;

public class Challenge
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 题目名称
    /// </summary>
    [Required(ErrorMessage = "标题是必需的")]
    [MinLength(1, ErrorMessage = "标题过短")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目内容
    /// </summary>
    [Required(ErrorMessage = "题目内容是必需的")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用题目
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// 题目标签
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ChallengeTag Tag { get; set; } = ChallengeTag.Misc;

    /// <summary>
    /// 题目提示，用";"分隔
    /// </summary>
    public string Hints { get; set; } = string.Empty;

    /// <summary>
    /// 镜像名称与标签
    /// </summary>
    [JsonIgnore]
    public string? ContainerImage { get; set; } = string.Empty;

    /// <summary>
    /// 运行内存限制 (MB)
    /// </summary>
    [JsonIgnore]
    public int? MemoryLimit { get; set; } = 64;

    /// <summary>
    /// CPU 运行数量限制
    /// </summary>
    [JsonIgnore]
    public int? CPUCount { get; set; } = 1;

    /// <summary>
    /// 镜像暴露端口
    /// </summary>
    [JsonIgnore]
    public int? ContainerExposePort { get; set; } = 80;

    /// <summary>
    /// 解决题目人数
    /// </summary>
    [Required]
    [JsonIgnore]
    public int AcceptedUserCount { get; set; } = 0;

    /// <summary>
    /// 提交答案的数量
    /// </summary>
    [Required]
    [JsonIgnore]
    public int SubmissionCount { get; set; } = 0;

    /// <summary>
    /// 初始分数
    /// </summary>
    [Required]
    public int OriginalScore { get; set; } = 500;

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
    public int Difficulty { get; set; } = 100;

    /// <summary>
    /// 题目类型
    /// </summary>
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;

    /// <summary>
    /// 下载文件名称
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 当前题目分值
    /// </summary>
    [NotMapped]
    public int CurrentScore
    {
        get
        {
            return (int)Math.Floor(
                OriginalScore * (MinScoreRate +
                    (1.0 - MinScoreRate) * Math.Exp((1.0 - AcceptedUserCount) / Difficulty)
                ));
        }
    }

    #region Db Relationship

    public List<FlagContext> Flags { get; set; } = new();

    /// <summary>
    /// 提交
    /// </summary>
    public List<Submission> Submissions { get; set; } = new();

    /// <summary>
    /// 比赛对象
    /// </summary>
    public Game Game { get; set; } = default!;

    public int GameId { get; set; }

    #endregion Db Relationship

    public Challenge Update(ChallengeModel model)
    {
        Type = model.Type;
        Title = model.Title;
        Content = model.Content;
        Tag = model.Tag;
        Hints = model.Hints;
        ContainerImage = model.ContainerImage;
        MemoryLimit = model.MemoryLimit ?? 64;
        CPUCount = model.CPUCount ?? 1;
        ContainerExposePort = model.ContainerExposePort ?? 80;
        OriginalScore = model.OriginalScore;
        MinScoreRate = model.MinScoreRate;
        Difficulty = model.Difficulty;
        FileName = model.FileName ?? "attachment";

        return this;
    }
}