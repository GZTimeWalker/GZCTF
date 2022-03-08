namespace CTFServer.Models.Request;

/// <summary>
/// 积分榜信息
/// </summary>
public class ScoreBoardMessageModel
{
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTimeOffset UpdateTime { get; set; }

    /// <summary>
    /// 排名
    /// </summary>
    public List<RankMessageModel>? Rank { get; set; }

    /// <summary>
    /// 前十名的时间线
    /// </summary>
    public List<ScoreBoardTimeLine>? TopDetail { get; set; }
}

/// <summary>
/// 积分榜时间轴
/// </summary>
public class ScoreBoardTimeLine
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 时间线
    /// </summary>
    public List<TimeLineModel>? TimeLine { get; set; }
}
