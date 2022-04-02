using System.Text.Json.Serialization;

namespace CTFServer.Models.Request.Game;

public class Scoreboard
{
    public List<ScoreboardItem> Items { get; set; } = new();

    public List<IGrouping<string, ChallengeInfo>> Challenges { get; set; } = new();
}

public class ScoreboardItem
{
    /// <summary>
    /// 队伍名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// 分数
    /// </summary>
    public int Score { get; set; }
    /// <summary>
    /// 排名
    /// </summary>
    public int Rank { get; set; }
    /// <summary>
    /// 已解出的题目数量
    /// </summary>
    public int SolvedCount { get; set; }
    /// <summary>
    /// 题目情况列表
    /// </summary>
    public List<ChallengeItem> Challenges { get; set; } = new();
}

public class ChallengeItem
{
    /// <summary>
    /// 题目 Id
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// 题目是否解出
    /// </summary>
    public bool IsSolved { get; set; }
    /// <summary>
    /// 一血、二血、三血或者其他
    /// </summary>
    [JsonPropertyName("rank")]
    public SubmissionType Type { get; set; }
}

public class ChallengeInfo
{
    /// <summary>
    /// 题目名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// 题目标签
    /// </summary>
    public ChallengeTag Tag { get; set; }
    /// <summary>
    /// 题目分值
    /// </summary>
    public bool Score { get; set; }
}
