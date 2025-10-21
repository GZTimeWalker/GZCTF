using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MemoryPack;

namespace GZCTF.Models.Request.Game;

/// <summary>
/// Scoreboard
/// </summary>
[MemoryPackable]
public partial class ScoreboardModel
{
    private Dictionary<ChallengeCategory, IEnumerable<ChallengeInfo>> _challenges = null!;

    /// <summary>
    /// Update time
    /// </summary>
    [Required]
    public DateTimeOffset UpdateTimeUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Blood bonus coefficient
    /// </summary>
    [Required]
    [JsonPropertyName("bloodBonus")]
    public long BloodBonusValue { get; set; } = BloodBonus.DefaultValue;

    /// <summary>
    /// Timeline of the top ten
    /// </summary>
    [JsonIgnore]
    public Dictionary<int, IEnumerable<TopTimeLine>> TimeLines { get; set; } = null!;

    /// <summary>
    /// Team information
    /// </summary>
    [JsonIgnore]
    public Dictionary<int, ScoreboardItem> Items { get; set; } = null!;

    /// <summary>
    /// Division information
    /// </summary>
    [JsonIgnore]
    public Dictionary<int, DivisionItem> Divisions { get; set; } = null!;

    /// <summary>
    /// List of top ten timelines
    /// </summary>
    [Required]
    [MemoryPackIgnore]
    [JsonPropertyName("timelines")]
    public IEnumerable<TimeLineItem> TimeLineList => TimeLines.Select(x => new TimeLineItem(x.Key, x.Value));

    /// <summary>
    /// List of team information
    /// </summary>
    [Required]
    [MemoryPackIgnore]
    [JsonPropertyName("items")]
    public IEnumerable<ScoreboardItem> ItemList => Items.Values;

    /// <summary>
    /// List of division information
    /// </summary>
    [Required]
    [MemoryPackIgnore]
    [JsonPropertyName("divisions")]
    public IEnumerable<DivisionItem> DivisionList => Divisions.Values;

    /// <summary>
    /// Challenge information
    /// </summary>
    [Required]
    public Dictionary<ChallengeCategory, IEnumerable<ChallengeInfo>> Challenges
    {
        get => _challenges;
        set
        {
            _challenges = value;
            ChallengeMap = value.Values.SelectMany(x => x)
                .ToDictionary(x => x.Id, x => x);
            ChallengeCount = ChallengeMap.Count;
        }
    }

    /// <summary>
    /// The map of challenge ID to challenge information
    /// </summary>
    [JsonIgnore]
    [MemoryPackIgnore]
    public Dictionary<int, ChallengeInfo> ChallengeMap { get; private set; } = null!;

    /// <summary>
    /// Number of challenges
    /// </summary>
    [Required]
    [MemoryPackIgnore]
    public int ChallengeCount { get; private set; }
}

public record TimeLineItem([Required] int DivisionId, [Required] IEnumerable<TopTimeLine> Teams);

[MemoryPackable]
public partial class DivisionItem
{
    /// <summary>
    /// Division ID
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// The name of the division.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Permissions associated with the division.
    /// </summary>
    [Required]
    public GamePermission DefaultPermissions { get; set; } = GamePermission.All;

    /// <summary>
    /// Challenge configs for this division.
    /// </summary>
    [Required]
    public Dictionary<int, DivisionChallengeItem> ChallengeConfigs { get; set; } = [];
}

[MemoryPackable]
public partial class DivisionChallengeItem
{
    /// <summary>
    /// Challenge ID
    /// </summary>
    [Required]
    public int ChallengeId { get; set; }

    /// <summary>
    /// Permissions for a specific challenge.
    /// </summary>
    [Required]
    public GamePermission Permissions { get; set; } = GamePermission.All;
}

[MemoryPackable]
public partial class TopTimeLine
{
    /// <summary>
    /// Team ID
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Team name
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Timeline
    /// </summary>
    [Required]
    public List<TimeLine> Items { get; set; } = null!;
}

[MemoryPackable]
public partial class TimeLine
{
    /// <summary>
    /// Time
    /// </summary>
    [Required]
    public DateTimeOffset Time { get; set; }

    /// <summary>
    /// Score
    /// </summary>
    [Required]
    public int Score { get; set; }
}

[MemoryPackable]
public partial class ScoreboardItem
{
    /// <summary>
    /// Team ID
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Team name
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Team Bio
    /// </summary>
    public string? Bio { get; set; } = string.Empty;

    /// <summary>
    /// Division of participation
    /// </summary>
    public int? DivisionId { get; set; }

    /// <summary>
    /// Team avatar
    /// </summary>
    public string? Avatar { get; set; } = string.Empty;

    /// <summary>
    /// Score
    /// </summary>
    [Required]
    public int Score { get; set; }

    /// <summary>
    /// Rank
    /// </summary>
    [Required]
    public int Rank { get; set; }

    /// <summary>
    /// Division rank
    /// </summary>
    public int? DivisionRank { get; set; }

    /// <summary>
    /// Last submission time
    /// </summary>
    [Required]
    public DateTimeOffset LastSubmissionTime { get; set; }

    /// <summary>
    /// List of solved challenges
    /// </summary>
    [Required]
    public List<ChallengeItem> SolvedChallenges { get; set; } = [];

    /// <summary>
    /// Number of solved challenges
    /// </summary>
    [Required]
    public int SolvedCount => SolvedChallenges.Count;

    /// <summary>
    /// Participant ID
    /// </summary>
    [JsonIgnore]
    [MemoryPackIgnore]
    public int ParticipantId { get; set; }

    /// <summary>
    /// Team information, used to generate the scoreboard
    /// </summary>
    [JsonIgnore]
    [MemoryPackIgnore]
    public Team? TeamInfo { get; set; }

    /// <summary>
    /// Team members participating in the game
    /// </summary>
    [JsonIgnore]
    [MemoryPackIgnore]
    public HashSet<UserInfo>? Participants { get; set; }
}

[MemoryPackable]
public partial class ChallengeItem
{
    /// <summary>
    /// Challenge ID
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Challenge score
    /// </summary>
    [Required]
    public int Score { get; set; }

    /// <summary>
    /// Submission type (unsolved, first blood, second blood, third blood, or others)
    /// </summary>
    [Required]
    [JsonPropertyName("type")]
    public SubmissionType Type { get; set; }

    /// <summary>
    /// Username of the solver
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Submission time for the challenge, used to calculate the timeline
    /// </summary>
    [Required]
    [JsonPropertyName("time")]
    public DateTimeOffset SubmitTimeUtc { get; set; }

    /// <summary>
    /// Participant ID
    /// </summary>
    [JsonIgnore]
    [MemoryPackIgnore]
    public int ParticipantId { get; set; }
}

[MemoryPackable]
public partial class ChallengeInfo
{
    /// <summary>
    /// Challenge ID
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Challenge title
    /// </summary>
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Challenge category
    /// </summary>
    [Required]
    public ChallengeCategory Category { get; set; }

    /// <summary>
    /// Challenge score
    /// </summary>
    [Required]
    public int Score { get; set; }

    /// <summary>
    /// Number of teams that solved the challenge
    /// </summary>
    [Required]
    [JsonPropertyName("solved")]
    public int SolvedCount { get; set; }

    /// <summary>
    /// The deadline of the challenge, null means no deadline
    /// </summary>
    [JsonPropertyName("deadline")]
    public DateTimeOffset? DeadlineUtc { get; set; }

    /// <summary>
    /// Bloods for the challenge
    /// </summary>
    [Required]
    public List<Blood> Bloods { get; set; } = [];

    /// <summary>
    /// Whether to disable blood bonus
    /// </summary>
    [Required]
    [NotMapped]
    [MemoryPackIgnore]
    public bool DisableBloodBonus { get; set; }
}

[MemoryPackable]
public partial class Blood
{
    /// <summary>
    /// Team ID
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Team name
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Team avatar
    /// </summary>
    public string? Avatar { get; set; } = string.Empty;

    /// <summary>
    /// Time when the blood was obtained
    /// </summary>
    public DateTimeOffset? SubmitTimeUtc { get; set; }
}
