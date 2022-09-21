using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CTFServer.Models.Request.Game;

public class GameDetailModel
{
    /// <summary>
    /// 题目信息
    /// </summary>
    public Dictionary<ChallengeTag, IEnumerable<ChallengeInfo>> Challenges { get; set; } = new();

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
}