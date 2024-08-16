using System.ComponentModel.DataAnnotations;
using GZCTF.Models.Request.Game;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// 题目详细信息（Edit）
/// </summary>
public class ChallengeEditDetailModel
{
    /// <summary>
    /// 题目Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 题目名称
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_TitleRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    [MinLength(1, ErrorMessageResourceName = nameof(Resources.Program.Model_TitleTooShort),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目标签
    /// </summary>
    [Required]
    public ChallengeTag Tag { get; set; } = ChallengeTag.Misc;

    /// <summary>
    /// 题目类型
    /// </summary>
    [Required]
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;

    /// <summary>
    /// 题目提示
    /// </summary>
    public List<string> Hints { get; set; } = [];

    /// <summary>
    /// Flag 模版，用于根据 Token 和题目、比赛信息生成 Flag
    /// </summary>
    [MaxLength(Limits.MaxFlagTemplateLength, ErrorMessageResourceName = nameof(Resources.Program.Model_FlagTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? FlagTemplate { get; set; }

    /// <summary>
    /// 是否启用题目
    /// </summary>
    [Required]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 通过人数
    /// </summary>
    [Required]
    public int AcceptedCount { get; set; }

    /// <summary>
    /// 统一文件名（仅用于动态附件）
    /// </summary>
    public string? FileName { get; set; } = string.Empty;

    /// <summary>
    /// 题目附件（动态附件存放于 FlagInfoModel）
    /// </summary>
    public Attachment? Attachment { get; set; }

    /// <summary>
    /// 测试容器
    /// </summary>
    public ContainerInfoModel? TestContainer { get; set; }

    /// <summary>
    /// 题目 Flag 信息
    /// </summary>
    [Required]
    public List<FlagInfoModel> Flags { get; set; } = [];

    /// <summary>
    /// 镜像名称与标签
    /// </summary>
    [Required]
    public string? ContainerImage { get; set; } = string.Empty;

    /// <summary>
    /// 运行内存限制 (MB)
    /// </summary>
    [Required]
    public int? MemoryLimit { get; set; } = 64;

    /// <summary>
    /// CPU 限制 (0.1 CPUs)
    /// </summary>
    [Required]
    public int? CPUCount { get; set; } = 1;

    /// <summary>
    /// 存储限制 (MB)
    /// </summary>
    [Required]
    public int? StorageLimit { get; set; } = 256;

    /// <summary>
    /// 镜像暴露端口
    /// </summary>
    [Required]
    public int? ContainerExposePort { get; set; } = 80;

    /// <summary>
    /// 是否需要记录访问流量
    /// </summary>
    public bool? EnableTrafficCapture { get; set; } = false;

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
    public double Difficulty { get; set; } = 3;

    internal static ChallengeEditDetailModel FromChallenge(GameChallenge chal) =>
        new()
        {
            Id = chal.Id,
            Title = chal.Title,
            Content = chal.Content,
            Tag = chal.Tag,
            Type = chal.Type,
            FlagTemplate = chal.FlagTemplate,
            Hints = chal.Hints ?? [],
            IsEnabled = chal.IsEnabled,
            ContainerImage = chal.ContainerImage,
            MemoryLimit = chal.MemoryLimit,
            CPUCount = chal.CPUCount,
            StorageLimit = chal.StorageLimit,
            ContainerExposePort = chal.ContainerExposePort,
            EnableTrafficCapture = chal.EnableTrafficCapture,
            OriginalScore = chal.OriginalScore,
            MinScoreRate = chal.MinScoreRate,
            Difficulty = chal.Difficulty,
            FileName = chal.FileName,
            AcceptedCount = chal.AcceptedCount,
            Attachment = chal.Attachment,
            TestContainer = chal.TestContainer is null ? null : ContainerInfoModel.FromContainer(chal.TestContainer),
            Flags = chal.Flags.Select(FlagInfoModel.FromFlagContext).ToList()
        };
}
