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

    [JsonPropertyName("commonSolves")]
    public int CommonSolves { get; set; }
}
