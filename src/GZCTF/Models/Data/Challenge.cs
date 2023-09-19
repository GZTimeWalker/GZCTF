using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GZCTF.Models.Data;

public class Challenge
{
    [Key]
    [Required]
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
    /// 题目标签
    /// </summary>
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ChallengeTag Tag { get; set; } = ChallengeTag.Misc;

    /// <summary>
    /// 题目类型，创建后不可更改
    /// </summary>
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;

    /// <summary>
    /// 题目提示
    /// </summary>
    public List<string>? Hints { get; set; }
    
    /// <summary>
    /// 是否启用题目
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Flag 模版，用于根据 Token 和题目、比赛信息生成 Flag
    /// </summary>
    public string? FlagTemplate { get; set; }

    /// <summary>
    /// 解决题目人数
    /// </summary>
    [Required]
    public int AcceptedCount { get; set; }

    /// <summary>
    /// 提交答案的数量
    /// </summary>
    [Required]
    public int SubmissionCount { get; set; }

    /// <summary>
    /// 镜像名称与标签
    /// </summary>
    public string? ContainerImage { get; set; } = string.Empty;

    /// <summary>
    /// 运行内存限制 (MB)
    /// </summary>
    public int? MemoryLimit { get; set; } = 64;

    /// <summary>
    /// 存储限制 (MB)
    /// </summary>
    public int? StorageLimit { get; set; } = 256;

    /// <summary>
    /// CPU 限制 (0.1 CPUs)
    /// </summary>
    public int? CPUCount { get; set; } = 1;

    /// <summary>
    /// 镜像暴露端口
    /// </summary>
    public int? ContainerExposePort { get; set; } = 80;
    
    /// <summary>
    /// 下载文件名称，仅用于动态附件统一文件名
    /// </summary>
    public string? FileName { get; set; } = "attachment";
    
    /// <summary>
    /// 并发检查
    /// </summary>
    [JsonIgnore]
    [ConcurrencyCheck]
    public Guid ConcurrencyStamp { get; set; }
    
    internal string GenerateTestFlag()
    {
        if (string.IsNullOrEmpty(FlagTemplate))
            return "flag{GZCTF_dynamic_flag_test}";

        if (FlagTemplate.Contains("[GUID]"))
            return FlagTemplate.Replace("[GUID]", Guid.NewGuid().ToString("D"));

        if (FlagTemplate.StartsWith("[LEET]"))
            return Codec.Leet.LeetFlag(FlagTemplate[6..]);

        return Codec.Leet.LeetFlag(FlagTemplate);
    }
    
    #region Db Relationship

    /// <summary>
    /// 题目附件 Id
    /// </summary>
    public int? AttachmentId { get; set; }

    /// <summary>
    /// 题目附件（动态附件存放于 FlagContext）
    /// </summary>
    public Attachment? Attachment { get; set; }

    /// <summary>
    /// 测试容器 Id
    /// </summary>
    public Guid? TestContainerId { get; set; }

    /// <summary>
    /// 测试容器
    /// </summary>
    public Container? TestContainer { get; set; }
    
    #endregion
}