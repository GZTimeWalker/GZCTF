using System.Text.Json.Serialization;

namespace GZCTF.Models.Response.Admin;

public class ReviewAnalyticsModel
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("likes")]
    public int Likes { get; set; }

    [JsonPropertyName("dislikes")]
    public int Dislikes { get; set; }

    [JsonPropertyName("topLiked")]
    public TopChallengeModel[] TopLiked { get; set; } = [];

    [JsonPropertyName("topDisliked")]
    public TopChallengeModel[] TopDisliked { get; set; } = [];
}

public class TopChallengeModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }
}
