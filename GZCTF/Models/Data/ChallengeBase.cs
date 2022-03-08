using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CTFServer.Models;

public class ChallengeBase
{
    /// <summary>
    /// 谜题名称
    /// </summary>
    [Required(ErrorMessage = "标题是必需的")]
    [MinLength(1, ErrorMessage = "标题过短")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 谜题内容
    /// </summary>
    [Required(ErrorMessage = "题目内容是必需的")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 客户端 js
    /// </summary>
    public string ClientJS { get; set; } = string.Empty;

    /// <summary>
    /// 谜题答案
    /// </summary>
    [Required(ErrorMessage = "答案是必需的")]
    [MinLength(6, ErrorMessage = "答案过短")]
    [MaxLength(50, ErrorMessage = "答案过长")]
    [RegularExpression("^msc{[a-zA-Z0-9_-]+}$", ErrorMessage = "答案格式错误")]
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// 访问等级
    /// </summary>
    [Required]
    public int AccessLevel { get; set; } = 0;

    /// <summary>
    /// 解决谜题人数
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
    /// 升级访问权限（为0则不变）
    /// </summary>
    [Required]
    public int UpgradeAccessLevel { get; set; } = 0;
}
