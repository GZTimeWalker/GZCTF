using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CTFServer.Models.Request.Game;

public class GameTeamDetailModel
{
    /// <summary>
    /// 积分榜信息
    /// </summary>
    [Required]
    [JsonPropertyName("rank")]
    public ScoreboardItem ScoreboardItem { get; set; } = default!;

    /// <summary>
    /// 队伍 Token
    /// </summary>
    [Required]
    public string TeamToken { get; set; } = string.Empty;
}