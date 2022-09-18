using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Request.Edit;

/// <summary>
/// 题目更新信息（Edit）
/// </summary>
public class ChallengeUpdateModel
{
    /// <summary>
    /// 题目名称
    /// </summary>
    [MinLength(1, ErrorMessage = "标题过短")]
    public string? Title { get; set; }

    /// <summary>
    /// 题目内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Flag 模版，用于根据 Token 和题目、比赛信息生成 Flag
    /// </summary>
    [MaxLength(120, ErrorMessage = "flag 字符串过长")]
    public string? FlagTemplate { get; set; }

    /// <summary>
    /// 题目标签
    /// </summary>
    public ChallengeTag? Tag { get; set; }

    /// <summary>
    /// 题目提示
    /// </summary>
    public List<string>? Hints { get; set; }

    /// <summary>
    /// 是否启用题目
    /// </summary>
    public bool? IsEnabled { get; set; }

    #region Container

    /// <summary>
    /// 镜像名称与标签
    /// </summary>
    public string? ContainerImage { get; set; }

    /// <summary>
    /// 运行内存限制 (MB)
    /// </summary>
    public int? MemoryLimit { get; set; }

    /// <summary>
    /// CPU 运行数量限制
    /// </summary>
    public int? CPUCount { get; set; }

    /// <summary>
    /// 镜像暴露端口
    /// </summary>
    public int? ContainerExposePort { get; set; }

    /// <summary>
    /// 是否为特权容器
    /// </summary>
    public bool? PrivilegedContainer { get; set; } = false;

    #endregion Container

    #region Score

    /// <summary>
    /// 初始分数
    /// </summary>
    public int? OriginalScore { get; set; }

    /// <summary>
    /// 最低分数比例
    /// </summary>
    [Range(0, 1)]
    public double? MinScoreRate { get; set; }

    /// <summary>
    /// 难度系数
    /// </summary>
    public double? Difficulty { get; set; }

    #endregion Score

    /// <summary>
    /// 统一文件名
    /// </summary>
    public string? FileName { get; set; }
}