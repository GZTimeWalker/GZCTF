using System.Text.Json.Serialization;

namespace GZCTF.Models.Response.Account;

public class UserStatsModel
{
    [JsonPropertyName("totalSolves")]
    public int TotalSolves { get; set; }

    [JsonPropertyName("totalFirstBloods")]
    public int TotalFirstBloods { get; set; }

    [JsonPropertyName("gamesParticipated")]
    public int GamesParticipated { get; set; }

    /// <summary>Solve count per challenge category name.</summary>
    [JsonPropertyName("solvesByCategory")]
    public Dictionary<string, int> SolvesByCategory { get; set; } = [];

    [JsonPropertyName("games")]
    public List<GameStatItem> Games { get; set; } = [];
}

public class GameStatItem
{
    [JsonPropertyName("gameId")]
    public int GameId { get; set; }

    [JsonPropertyName("gameTitle")]
    public string GameTitle { get; set; } = string.Empty;

    [JsonPropertyName("endTimeUtc")]
    public DateTimeOffset EndTimeUtc { get; set; }

    [JsonPropertyName("solves")]
    public int Solves { get; set; }
}
