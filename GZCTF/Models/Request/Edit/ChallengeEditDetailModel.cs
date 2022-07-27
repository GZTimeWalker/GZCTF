using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Request.Edit;

/// <summary>
/// 题目详细信息（Edit）
/// </summary>
public class ChallengeEditDetailModel
{
    /// <summary>
    /// 题目名称
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "标题过短")]
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
    /// 题目提示，用";"分隔
    /// </summary>
    public string Hints { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用题目
    /// </summary>
    [Required]
    public bool IsEnabled { get; set; }

    #region Container

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
    /// CPU 运行数量限制
    /// </summary>
    [Required]
    public int? CPUCount { get; set; } = 1;

    /// <summary>
    /// 镜像暴露端口
    /// </summary>
    [Required]
    public int? ContainerExposePort { get; set; } = 80;

    #endregion Container

    #region Score

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

    #endregion Score

    /// <summary>
    /// 统一文件名
    /// </summary>
    [Required]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 统一文件名
    /// </summary>
    [Required]
    public int AcceptedCount { get; set; } = 0;

    /// <summary>
    /// 题目 Flag 信息
    /// </summary>
    [Required]
    public FlagInfoModel[] Flags { get; set; } = Array.Empty<FlagInfoModel>();

    public static ChallengeEditDetailModel FromChallenge(Challenge chal)
        => new()
        {
            Title = chal.Title,
            Content = chal.Content,
            Tag = chal.Tag,
            Type = chal.Type,
            Hints = chal.Hints,
            IsEnabled = chal.IsEnabled,
            ContainerImage = chal.ContainerImage,
            MemoryLimit = chal.MemoryLimit,
            CPUCount = chal.CPUCount,
            ContainerExposePort = chal.ContainerExposePort,
            OriginalScore = chal.OriginalScore,
            MinScoreRate = chal.MinScoreRate,
            Difficulty = chal.Difficulty,
            FileName = chal.FileName,
            AcceptedCount = chal.AcceptedCount,
            Flags = (from flag in chal.Flags
                     select FlagInfoModel.FromFlagContext(flag))
                    .ToArray()
        };
}