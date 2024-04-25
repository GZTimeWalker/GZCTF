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
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_TitleRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    [MinLength(1, ErrorMessageResourceName = nameof(Resources.Program.Model_TitleTooShort),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目内容
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_ContentRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
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

    /// <summary>
    /// Flag 模版，用于根据 Token 和题目、比赛信息生成 Flag
    /// </summary>
    [MaxLength(Limits.MaxFlagTemplateLength)]
    public string? FlagTemplate { get; set; }

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

    /// <summary>
    /// 题目对应的 Flag 列表
    /// </summary>
    public List<FlagContext> Flags { get; set; } = [];

    /// <summary>
    /// 为参赛对象生成动态 Flag
    /// </summary>
    /// <param name="part"></param>
    /// <returns></returns>
    internal string GenerateDynamicFlag(Participation part)
    {
        if (string.IsNullOrEmpty(FlagTemplate))
            return $"flag{Guid.NewGuid():B}";

        if (FlagTemplate.Contains("[GUID]"))
            return FlagTemplate.Replace("[GUID]", Guid.NewGuid().ToString("D"));

        if (!FlagTemplate.Contains("[TEAM_HASH]"))
            return Codec.Leet.LeetFlag(FlagTemplate);

        var flag = FlagTemplate;
        if (FlagTemplate.StartsWith("[LEET]"))
            flag = Codec.Leet.LeetFlag(FlagTemplate[6..]);

        //   Using the signature private key of the game to generate a hash for the
        // team is not a wise and sufficiently secure choice. Moreover, this private
        // key should not exist outside of any backend systems, even if it is encrypted
        // with a XOR key in a configuration file or provided to the organizers (admin)
        // for third-party flag calculation and external distribution.
        //   To address this issue, one possible solution is to use a salted hash of
        // the private key as the salt for the team's hash.
        var hash = $"{part.Token}::{part.Game.TeamHashSalt}::{Id}".ToSHA256String();
        return flag.Replace("[TEAM_HASH]", hash[12..24]);
    }

    /// <summary>
    /// 直接生成动态 Flag
    /// </summary>
    internal string GenerateDynamicFlag()
    {
        if (string.IsNullOrEmpty(FlagTemplate))
            return $"flag{Guid.NewGuid():B}";

        var guid = Guid.NewGuid();
        if (FlagTemplate.Contains("[GUID]"))
            return FlagTemplate.Replace("[GUID]", guid.ToString("D"));

        if (!FlagTemplate.Contains("[TEAM_HASH]"))
            return Codec.Leet.LeetFlag(FlagTemplate);

        var flag = FlagTemplate;
        if (FlagTemplate.StartsWith("[LEET]"))
            flag = Codec.Leet.LeetFlag(FlagTemplate[6..]);

        return flag.Replace("[TEAM_HASH]", guid.ToString("N")[..12]);
    }

    internal string GenerateTestFlag()
    {
        if (string.IsNullOrEmpty(FlagTemplate))
            return "flag{GZCTF_dynamic_flag_test}";

        if (FlagTemplate.Contains("[GUID]"))
            return FlagTemplate.Replace("[GUID]", Guid.NewGuid().ToString("D"));

        return Codec.Leet.LeetFlag(FlagTemplate.StartsWith("[LEET]") ? FlagTemplate[6..] : FlagTemplate);
    }

    #endregion
}
