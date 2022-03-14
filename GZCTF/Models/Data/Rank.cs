using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CTFServer.Models;

public class Rank
{
    [Key]
    public int Id { get; set; }

    [JsonIgnore]
    public Challenge? Challenge { get; set; }

    [JsonIgnore]
    public Team? First { get; set; }

    [JsonPropertyName("first")]
    public string? FirstTeamName { get; set; }

    [JsonIgnore]
    public Team? Second { get; set; }

    [JsonPropertyName("second")]
    public string? SecondTeamName { get; set; }

    [JsonIgnore]
    public Team? Third { get; set; }

    [JsonPropertyName("third")]
    public string? ThirdTeamName { get; set; }
}
