using System.ComponentModel.DataAnnotations;
using GZCTF.Extensions;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// 题目更新信息（Edit）
/// </summary>
public class ChallengeUpdateModel
{
    /// <summary>
    /// 题目名称
    /// </summary>
    [MinLength(1, ErrorMessageResourceName = nameof(Resources.Program.Model_TitleTooShort),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? Title { get; set; }

    /// <summary>
    /// 题目内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Flag 模版，用于根据 Token 和题目、比赛信息生成 Flag
    /// </summary>
    [MaxLength(Limits.MaxFlagTemplateLength, ErrorMessageResourceName = nameof(Resources.Program.Model_FlagTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
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

    /// <summary>
    /// 统一文件名
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// 镜像名称与标签
    /// </summary>
    public string? ContainerImage { get; set; }

    /// <summary>
    /// 运行内存限制 (MB)
    /// </summary>
    [Range(32, 1048576, ErrorMessageResourceName = nameof(Resources.Program.Model_OutOfRange),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public int? MemoryLimit { get; set; }

    /// <summary>
    /// CPU 限制 (0.1 CPUs)
    /// </summary>
    [Range(1, 1024, ErrorMessageResourceName = nameof(Resources.Program.Model_OutOfRange),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public int? CPUCount { get; set; }

    /// <summary>
    /// 存储限制 (MB)
    /// </summary>
    [Range(128, 1048576, ErrorMessageResourceName = nameof(Resources.Program.Model_OutOfRange),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public int? StorageLimit { get; set; }

    /// <summary>
    /// 镜像暴露端口
    /// </summary>
    public int? ContainerExposePort { get; set; }

    /// <summary>
    /// 是否需要记录访问流量
    /// </summary>
    public bool? EnableTrafficCapture { get; set; } = false;

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

    /// <summary>
    /// 提示是否存在更新
    /// </summary>
    /// <param name="originalHash">原有哈希</param>
    /// <returns></returns>
    internal bool IsHintUpdated(int? originalHash) =>
        Hints is not null && Hints.GetSetHashCode() != originalHash;

    /// <summary>
    /// 是否为有效的 Flag 模板
    /// </summary>
    /// <returns></returns>
    internal bool IsValidFlagTemplate()
    {
        if (string.IsNullOrWhiteSpace(FlagTemplate))
            return false;

        return FlagTemplate.Contains("[GUID]") ||
               FlagTemplate.Contains("[TEAM_HASH]") ||
               Codec.Leet.LeetEntropy(FlagTemplate) >= 32.0;
    }
}
