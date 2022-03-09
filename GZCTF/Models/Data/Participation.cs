using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CTFServer.Models;

public class Participation
{
    [Key]
    [JsonIgnore]
    public int Id { get; set; }

    /// <summary>
    /// 比赛 Token
    /// </summary>
    [Required]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 当前分数
    /// </summary>
    [Required]
    public int Score { get; set; } = 0;

    #region Db Relationship

    /// <summary>
    /// 赛题实例
    /// </summary>
    public List<Instance> Instances { get; set; } = new();

    /// <summary>
    /// 提交
    /// </summary>
    public List<Submission> Submissions { get; set; } = new();

    #endregion Db Relationship
}
