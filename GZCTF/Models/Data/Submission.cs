using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models;

[Index(nameof(UserId), nameof(PuzzleId))]
public class Submission
{
    [Key]
    [JsonIgnore]
    public int Id { get; set; }

    /// <summary>
    /// 提交的答案字符串
    /// </summary>
    [MaxLength(50)]
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// 提交的答案是否正确
    /// </summary>
    public bool Solved { get; set; } = false;

    /// <summary>
    /// 提交的得分
    /// </summary>
    public int Score { get; set; } = 0;

    /// <summary>
    /// 答案提交的时间
    /// </summary>
    [JsonPropertyName("time")]
    public DateTimeOffset SubmitTimeUTC { get; set; } = DateTimeOffset.Parse("1970-01-01T00:00:00Z");

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = "Anonymous";

    #region 数据库关系

    /// <summary>
    /// 用户数据库Id
    /// </summary>
    [JsonIgnore]
    public string? UserId { get; set; }

    /// <summary>
    /// 用户
    /// </summary>
    [JsonIgnore]
    public UserInfo? User { get; set; }

    /// <summary>
    /// 谜题数据库Id
    /// </summary>
    public int PuzzleId { get; set; }

    /// <summary>
    /// 谜题
    /// </summary>
    [JsonIgnore]
    public Puzzle? Puzzle { get; set; }

    #endregion 数据库关系
}
