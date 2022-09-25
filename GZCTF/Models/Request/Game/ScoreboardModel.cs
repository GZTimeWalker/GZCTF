using CTFServer.Models.Request.Info;
using System.Text.Json.Serialization;

namespace CTFServer.Models.Request.Game;

/// <summary>
/// 排行榜
/// </summary>
public class ScoreboardModel
{
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTimeOffset UpdateTimeUTC { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 参赛组织
    /// </summary>
    public IEnumerable<string>? Organizations { get; set; }

    /// <summary>
    /// 前十名的时间线
    /// </summary>
    public IEnumerable<TopTimeLine> TimeLine { get; set; } = default!;

    /// <summary>
    /// 队伍信息
    /// </summary>
    public IEnumerable<ScoreboardItem> Items { get; set; } = default!;

    /// <summary>
    /// 题目信息
    /// </summary>
    public Dictionary<ChallengeTag, IEnumerable<ChallengeInfo>> Challenges { get; set; } = default!;
}

public class TopTimeLine
{
    /// <summary>
    /// 队伍Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 队伍名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 时间线
    /// </summary>
    public IEnumerable<TimeLine> Items { get; set; } = default!;
}

public class TimeLine
{
    /// <summary>
    /// 时间
    /// </summary>
    public DateTimeOffset Time { get; set; }

    /// <summary>
    /// 得分
    /// </summary>
    public int Score { get; set; }
}

public class ScoreboardItem
{
    /// <summary>
    /// 队伍Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 队伍名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 参赛所属组织
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// 队伍头像
    /// </summary>
    public string? Avatar { get; set; } = string.Empty;

    /// <summary>
    /// 分数
    /// </summary>
    public int Score => Challenges.Sum(c => c.Score);

    /// <summary>
    /// 排名
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// 已解出的题目数量
    /// </summary>
    public int SolvedCount { get; set; }

    /// <summary>
    /// 得分时间
    /// </summary>
    public DateTimeOffset LastSubmissionTime { get; set; }

    /// <summary>
    /// 题目情况列表
    /// </summary>
    public IEnumerable<ChallengeItem> Challenges { get; set; } = Array.Empty<ChallengeItem>();
}

public class ChallengeItem
{
    /// <summary>
    /// 题目 Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 题目分值
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// 未解出、一血、二血、三血或者其他
    /// </summary>
    [JsonPropertyName("type")]
    public SubmissionType Type { get; set; }

    /// <summary>
    /// 解题用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 题目提交的时间，为了计算时间线
    /// </summary>
    [JsonPropertyName("time")]
    public DateTimeOffset? SubmitTimeUTC { get; set; }
}

public class ChallengeInfo
{
    /// <summary>
    /// 题目Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 题目名称
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目标签
    /// </summary>
    public ChallengeTag Tag { get; set; }

    /// <summary>
    /// 题目分值
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// 解出队伍数量
    /// </summary>
    [JsonPropertyName("solved")]
    public int SolvedCount { get; set; }

    /// <summary>
    /// 题目三血
    /// </summary>
    public Blood?[] Bloods { get; set; } = default!;
}

public class Blood
{
    /// <summary>
    /// 队伍Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 队伍名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 队伍头像
    /// </summary>
    public string? Avatar { get; set; } = string.Empty;

    /// <summary>
    /// 获得此血的时间
    /// </summary>
    public DateTimeOffset? SubmitTimeUTC { get; set; }
}