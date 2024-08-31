using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GZCTF.Models.Request.Game;

public class GameDetailModel
{
    /// <summary>
    /// 题目信息
    /// </summary>
    public Dictionary<ChallengeTag, IEnumerable<ChallengeInfo>> Challenges { get; set; } = new();

    /// <summary>
    /// 题目数量
    /// </summary>
    public int ChallengeCount { get; set; }

    /// <summary>
    /// 积分榜信息
    /// </summary>
    [JsonPropertyName("rank")]
    public ScoreboardItem? ScoreboardItem { get; set; }

    /// <summary>
    /// 队伍 Token
    /// </summary>
    [Required]
    public string TeamToken { get; set; } = string.Empty;

    /// <summary>
    /// 是否需要提交 Writeup
    /// </summary>
    [Required]
    public bool WriteupRequired { get; set; } = false;

    /// <summary>
    /// Writeup 提交截止时间
    /// </summary>
    [Required]
    public DateTimeOffset WriteupDeadline { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);
}
