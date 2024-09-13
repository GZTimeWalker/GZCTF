using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MemoryPack;

namespace GZCTF.Models.Request.Game;

/// <summary>
/// 排行榜
/// </summary>
[MemoryPackable]
public partial class ScoreboardModel
{
    /// <summary>
    /// 更新时间
    /// </summary>
    [Required]
    public DateTimeOffset UpdateTimeUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 三血加分系数
    /// </summary>
    [Required]
    [JsonPropertyName("bloodBonus")]
    public long BloodBonusValue { get; set; } = BloodBonus.DefaultValue;

    /// <summary>
    /// 前十名的时间线
    /// </summary>
    public Dictionary<string, IEnumerable<TopTimeLine>> TimeLines { get; set; } = default!;

    /// <summary>
    /// 队伍信息
    /// </summary>
    [JsonIgnore]
    public Dictionary<int, ScoreboardItem> Items { get; set; } = default!;

    /// <summary>
    /// 队伍信息列表
    /// </summary>
    [MemoryPackIgnore]
    [JsonPropertyName("items")]
    public IEnumerable<ScoreboardItem> ItemList => Items.Values;

    /// <summary>
    /// 题目信息
    /// </summary>
    public Dictionary<ChallengeCategory, IEnumerable<ChallengeInfo>> Challenges
    {
        get => _challenges;
        set
        {
            _challenges = value;
            ChallengeCount = value.Values.Select(x => x.Count()).Sum();
        }
    }

    private Dictionary<ChallengeCategory, IEnumerable<ChallengeInfo>> _challenges = default!;

    /// <summary>
    /// 题目数量
    /// </summary>
    [MemoryPackIgnore]
    public int ChallengeCount { get; private set; }
}

[MemoryPackable]
public partial class TopTimeLine
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
    public List<TimeLine> Items { get; set; } = default!;
}

[MemoryPackable]
public partial class TimeLine
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

[MemoryPackable]
public partial class ScoreboardItem
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
    /// 队伍 Bio
    /// </summary>
    public string? Bio { get; set; } = string.Empty;

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
    public int Score { get; set; }

    /// <summary>
    /// 排名
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// 参赛所属组织排名
    /// </summary>
    public int? OrganizationRank { get; set; }

    /// <summary>
    /// 得分时间
    /// </summary>
    public DateTimeOffset LastSubmissionTime { get; set; }

    /// <summary>
    /// 解出的题目列表
    /// </summary>
    public List<ChallengeItem> SolvedChallenges { get; set; } = [];

    /// <summary>
    /// 已解出的题目数量
    /// </summary>
    public int SolvedCount => SolvedChallenges.Count;

    /// <summary>
    /// 参与对象 Id
    /// </summary>
    [JsonIgnore]
    [MemoryPackIgnore]
    public int ParticipantId { get; set; }

    /// <summary>
    /// 队伍信息，用于生成排行榜
    /// </summary>
    [JsonIgnore]
    [MemoryPackIgnore]
    public Team? TeamInfo { get; set; }
}

[MemoryPackable]
public partial class ChallengeItem
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
    public DateTimeOffset SubmitTimeUtc { get; set; }

    /// <summary>
    /// 参与对象 Id
    /// </summary>
    [JsonIgnore]
    [MemoryPackIgnore]
    public int ParticipantId { get; set; }
}

[MemoryPackable]
public partial class ChallengeInfo
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
    /// 题目类别
    /// </summary>
    public ChallengeCategory Category { get; set; }

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
    public List<Blood> Bloods { get; set; } = [];
}

[MemoryPackable]
public partial class Blood
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
    public DateTimeOffset? SubmitTimeUtc { get; set; }
}
