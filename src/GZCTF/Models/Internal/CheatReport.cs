using System.Text.Json.Serialization;

namespace GZCTF.Models.Internal;

public class CheatReport
{
    [JsonPropertyName("generatedAt")]
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("ipAnalysis")]
    public List<IpAnalysisResult> IpAnalysis { get; set; } = new();

    [JsonPropertyName("abnormalSolves")]
    public List<AbnormalSolveResult> AbnormalSolves { get; set; } = new();

    [JsonPropertyName("sequenceSuspects")]
    public List<SequenceSuspectResult> SequenceSuspects { get; set; } = new();

    [JsonPropertyName("suspicionList")]
    public List<SuspicionRecordResult> SuspicionList { get; set; } = new();
}

public class SuspicionRecordResult
{
    [JsonPropertyName("teamId")]
    public int TeamId { get; set; }

    [JsonPropertyName("participationId")]
    public int ParticipationId { get; set; }

    [JsonPropertyName("status")]
    public ParticipationStatus Status { get; set; }

    [JsonPropertyName("teamName")]
    public string TeamName { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("events")]
    public List<SuspicionEventResult> Events { get; set; } = new();
}

public class SuspicionEventResult
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("scoreDelta")]
    public int ScoreDelta { get; set; }

    [JsonPropertyName("details")]
    public string Details { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public DateTimeOffset Time { get; set; }
}

public class IpAnalysisResult
{
    [JsonPropertyName("teamId")]
    public int TeamId { get; set; }

    [JsonPropertyName("teamName")]
    public string TeamName { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "CrossTeamIP", "SharedIP"

    [JsonPropertyName("details")]
    public string Details { get; set; } = string.Empty;

    [JsonPropertyName("relatedTeams")]
    public List<string> RelatedTeams { get; set; } = new();

    [JsonPropertyName("ip")]
    public string Ip { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public DateTimeOffset? Time { get; set; }
}

public class AbnormalSolveResult
{
    [JsonPropertyName("teamId")]
    public int TeamId { get; set; }

    [JsonPropertyName("teamName")]
    public string TeamName { get; set; } = string.Empty;

    [JsonPropertyName("challengeId")]
    public int ChallengeId { get; set; }

    [JsonPropertyName("challengeName")]
    public string ChallengeName { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "NoDownload", "NoContainer"

    [JsonPropertyName("details")]
    public string Details { get; set; } = string.Empty;

    [JsonPropertyName("solveTime")]
    public DateTimeOffset SolveTime { get; set; }
}

public class SequenceSuspectResult
{
    [JsonPropertyName("teamA")]
    public string TeamA { get; set; } = string.Empty;

    [JsonPropertyName("teamB")]
    public string TeamB { get; set; } = string.Empty;

    [JsonPropertyName("similarity")]
    public double Similarity { get; set; }

    [JsonPropertyName("timeCorrelation")]
    public double TimeCorrelation { get; set; }

    [JsonPropertyName("commonSolves")]
    public int CommonSolves { get; set; }

    [JsonPropertyName("details")]
    public string Details { get; set; } = string.Empty;

    [JsonPropertyName("detailedSolves")]
    public List<SequenceSuspectDetail> DetailedSolves { get; set; } = new();
}

public class SequenceSuspectDetail
{
    [JsonPropertyName("challengeName")]
    public string ChallengeName { get; set; } = string.Empty;

    [JsonPropertyName("timeA")]
    public DateTimeOffset TimeA { get; set; }

    [JsonPropertyName("timeB")]
    public DateTimeOffset TimeB { get; set; }

    [JsonPropertyName("timeDiff")]
    public double TimeDiff { get; set; }
}
