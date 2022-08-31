using System;
namespace CTFServer.Models.Request.Game;

public class GameTeamDetailModel
{
    /// <summary>
    /// 积分榜信息
    /// </summary>
    public ScoreboardItem ScoreboardItem { get; set; } = default!;

    /// <summary>
    /// 队伍 Token
    /// </summary>
    public string TeamToken { get; set; } = string.Empty;
}

