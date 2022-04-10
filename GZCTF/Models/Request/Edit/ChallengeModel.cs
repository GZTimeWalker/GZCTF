using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CTFServer.Models.Request.Edit;

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
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ChallengeTag Tag { get; set; } = ChallengeTag.Misc;

    /// <summary>
    /// 题目提示，用";"分隔
    /// </summary>
    public string Hints { get; set; } = string.Empty;

    /// <summary>
    /// 题目类型
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;

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
    #endregion

    #region Score
    /// <summary>
    /// 初始分数
    /// </summary>
    public int OriginalScore { get; set; } = 500;

    /// <summary>
    /// 最低分数
    /// </summary>
    public int MinScore { get; set; } = 300;

    /// <summary>
    /// 预期最大解出人数
    /// </summary>
    public int ExpectMaxCount { get; set; } = 100;

    /// <summary>
    /// 奖励人数
    /// </summary>
    public int AwardCount { get; set; } = 10;
    #endregion

    /// <summary>
    /// 统一文件名
    /// </summary>
    public string? FileName { get; set; } = string.Empty;

    /// <summary>
    /// Flag 及其对应附件
    /// </summary>
    public List<FlagInfoModel> Flags { get; set; } = new();
}

public class FlagInfoModel
{
    /// <summary>
    /// Flag文本
    /// </summary>
    public string Flag { get; set; } = string.Empty;
    /// <summary>
    /// Flag 对应附件（本地文件哈希）
    /// </summary>
    public string? FileHash { get; set; } = string.Empty;
    /// <summary>
    /// Flag 对应附件 (远程文件）
    /// </summary>
    public string? Url { get; set; } = string.Empty;
}