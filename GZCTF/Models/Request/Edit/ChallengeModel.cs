using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Request.Edit;

/// <summary>
/// 题目信息更改（Edit）
/// </summary>
public class ChallengeModel
{
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
    /// 题目标签
    /// </summary>
    public ChallengeTag Tag { get; set; } = ChallengeTag.Misc;

    /// <summary>
    /// 题目提示，用";"分隔
    /// </summary>
    public string Hints { get; set; } = string.Empty;

    /// <summary>
    /// 题目类型
    /// </summary>
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;

    /// <summary>
    /// 是否启用题目
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    #region Container

    /// <summary>
    /// 镜像名称与标签
    /// </summary>
    public string? ContainerImage { get; set; } = string.Empty;

    /// <summary>
    /// 运行内存限制 (MB)
    /// </summary>
    public int? MemoryLimit { get; set; } = 64;

    /// <summary>
    /// CPU 运行数量限制
    /// </summary>
    public int? CPUCount { get; set; } = 1;

    /// <summary>
    /// 镜像暴露端口
    /// </summary>
    public int? ContainerExposePort { get; set; } = 80;

    #endregion Container

    #region Score

    /// <summary>
    /// 初始分数
    /// </summary>
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

    #endregion Score

    /// <summary>
    /// 统一文件名
    /// </summary>
    public string? FileName { get; set; } = string.Empty;
}