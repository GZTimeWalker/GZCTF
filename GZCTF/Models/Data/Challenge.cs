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
    /// 题目答案
    /// </summary>
    [Required(ErrorMessage = "答案是必需的")]
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// 题目标签
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// 题目提示，用";"分隔
    /// </summary>
    public string Hints { get; set; } = string.Empty;

    /// <summary>
    /// 镜像名称与标签
    /// </summary>
    [JsonIgnore]
    public string ContainerImage { get; set; } = string.Empty;

    /// <summary>
    /// 运行内存限制 (MB)
    /// </summary>
    [JsonIgnore]
    public int MemoryLimit { get; set; } = 256;

    /// <summary>
    /// CPU 运行数量限制
    /// </summary>
    [JsonIgnore]
    public int CPUCount { get; set; } = 1;

    /// <summary>
    /// 镜像暴露端口
    /// </summary>
    [JsonIgnore]
    public int ContainerExposePort { get; set; } = 80;

    /// <summary>
    /// 解决题目人数
    /// </summary>
    [Required]
    [JsonIgnore]
    public int AcceptedUserCount { get; set; } = 0;

    /// <summary>
    /// 正确答案的数量
    /// </summary>
    [Required]
    [JsonIgnore]
    public int AcceptedCount { get; set; } = 0;

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
    /// 最低分数
    /// </summary>
    [Required]
    public int MinScore { get; set; } = 300;

    /// <summary>
    /// 预期最大解出人数
    /// </summary>
    [Required]
    public int ExpectMaxCount { get; set; } = 100;

    /// <summary>
    /// 奖励人数
    /// </summary>
    [Required]
    public int AwardCount { get; set; } = 10;

    /// <summary>
    /// 题目类型
    /// </summary>
    [Required]
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;

    /// <summary>
    /// 当前题目分值
    /// </summary>
    [NotMapped]
    public int CurrentScore
    {
        get
        {
            if (AcceptedUserCount <= AwardCount)
                return OriginalScore - AcceptedUserCount;
            if (AcceptedUserCount > ExpectMaxCount)
                return MinScore;
            var range = OriginalScore - AwardCount - MinScore;
            return (int)(OriginalScore - AwardCount
                - Math.Floor(range * (AcceptedUserCount - AwardCount) / (float)(ExpectMaxCount - AwardCount)));
        }
    }

    #region Db Relationship
    public List<FlagContext> Flags { get; set; } = new();

    /// <summary>
    /// 比赛对象
    /// </summary>
    public Game Game { get; set; } = default!;

    public int GameId { get; set; }
    #endregion Db Relationship
}
