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
    public Dictionary<string, IEnumerable<TopTimeLine>> TimeLines { get; set; } = null!;

    /// <summary>
    /// Team information
    /// </summary>
    [JsonIgnore]
    public Dictionary<int, ScoreboardItem> Items { get; set; } = null!;

    /// <summary>
    /// List of team information
    /// </summary>
    [MemoryPackIgnore]
    [JsonPropertyName("items")]
    public IEnumerable<ScoreboardItem> ItemList => Items.Values;

    /// <summary>
    /// Challenge information
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

    /// <summary>
    /// Number of challenges
    /// </summary>
    [MemoryPackIgnore]
    public int ChallengeCount { get; private set; }
}

[MemoryPackable]
public partial class TopTimeLine
{
    /// <summary>
    /// Team ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Team name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Timeline
    /// </summary>
    public List<TimeLine> Items { get; set; } = null!;
}

[MemoryPackable]
public partial class TimeLine
{
    /// <summary>
    /// Time
    /// </summary>
    public DateTimeOffset Time { get; set; }

    /// <summary>
    /// Score
    /// </summary>
    public int Score { get; set; }
}

[MemoryPackable]
public partial class ScoreboardItem
{
    /// <summary>
    /// Team ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Team name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Team Bio
    /// </summary>
    public string? Bio { get; set; } = string.Empty;

    /// <summary>
    /// Division of participation
    /// </summary>
    public string? Division { get; set; }

    /// <summary>
    /// Team avatar
    /// </summary>
    public string? Avatar { get; set; } = string.Empty;

    /// <summary>
    /// Score
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Rank
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Division rank
    /// </summary>
    public int? DivisionRank { get; set; }

    /// <summary>
    /// Last submission time
    /// </summary>
    public DateTimeOffset LastSubmissionTime { get; set; }

    /// <summary>
    /// List of solved challenges
    /// </summary>
    public List<ChallengeItem> SolvedChallenges { get; set; } = [];

    /// <summary>
    /// Number of solved challenges
    /// </summary>
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
    public int Id { get; set; }

    /// <summary>
    /// Challenge score
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Submission type (unsolved, first blood, second blood, third blood, or others)
    /// </summary>
    [JsonPropertyName("type")]
    public SubmissionType Type { get; set; }

    /// <summary>
    /// Username of the solver
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Submission time for the challenge, used to calculate the timeline
    /// </summary>
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
    public int Id { get; set; }

    /// <summary>
    /// Challenge title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Challenge category
    /// </summary>
    public ChallengeCategory Category { get; set; }

    /// <summary>
    /// Challenge score
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Number of teams that solved the challenge
    /// </summary>
    [JsonPropertyName("solved")]
    public int SolvedCount { get; set; }

    /// <summary>
    /// Bloods for the challenge
    /// </summary>
    public List<Blood> Bloods { get; set; } = [];

    /// <summary>
    /// Whether to disable blood bonus
    /// </summary>
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
    public int Id { get; set; }

    /// <summary>
    /// Team name
    /// </summary>
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
