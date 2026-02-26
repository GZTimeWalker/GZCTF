using System.Text.Json;
using System.Text.Json.Serialization;

namespace GZCTF.Models.Internal;

public sealed class DownloadEventLogMetadata
{
    public const string CurrentSchema = "gzctf.download-log.v1";
    public const int ValuesIndex = 4;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [JsonPropertyName("schema")]
    public string Schema { get; set; } = CurrentSchema;

    [JsonPropertyName("challengeId")]
    public int ChallengeId { get; set; }

    [JsonPropertyName("challengeTitle")]
    public string ChallengeTitle { get; set; } = string.Empty;

    [JsonPropertyName("actorTeamId")]
    public int ActorTeamId { get; set; }

    [JsonPropertyName("actorTeamName")]
    public string ActorTeamName { get; set; } = string.Empty;

    [JsonPropertyName("actorUserId")]
    public Guid? ActorUserId { get; set; }

    [JsonPropertyName("actorUserName")]
    public string? ActorUserName { get; set; }

    [JsonPropertyName("actorSource")]
    public string ActorSource { get; set; } = "anonymous";

    [JsonPropertyName("tokenType")]
    public string TokenType { get; set; } = "none";

    [JsonPropertyName("tokenAbuse")]
    public bool TokenAbuse { get; set; }

    [JsonPropertyName("tokenSourceTeamId")]
    public int? TokenSourceTeamId { get; set; }

    [JsonPropertyName("tokenSourceTeamName")]
    public string? TokenSourceTeamName { get; set; }

    [JsonPropertyName("tokenSourceUserId")]
    public Guid? TokenSourceUserId { get; set; }

    [JsonPropertyName("tokenSourceUserName")]
    public string? TokenSourceUserName { get; set; }

    public string Serialize() => JsonSerializer.Serialize(this, JsonOptions);

    public static bool TryParse(string? json, out DownloadEventLogMetadata metadata)
    {
        metadata = new();

        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            var parsed = JsonSerializer.Deserialize<DownloadEventLogMetadata>(json, JsonOptions);
            if (parsed is null)
                return false;

            if (!string.Equals(parsed.Schema, CurrentSchema, StringComparison.OrdinalIgnoreCase))
                return false;

            metadata = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
