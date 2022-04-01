using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CTFServer.Models.Request.Game;

public class GameDetailsModel
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 比赛标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 比赛描述
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 比赛详细介绍
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 开始时间
    /// </summary>
    [JsonPropertyName("start")]
    public DateTimeOffset StartTimeUTC { get; set; } = DateTimeOffset.Parse("1970-01-01T00:00:00Z");

    /// <summary>
    /// 结束时间
    /// </summary>
    [JsonPropertyName("end")]
    public DateTimeOffset EndTimeUTC { get; set; } = DateTimeOffset.Parse("1970-01-01T00:00:00Z");

    public static GameDetailsModel FromGame(Models.Game game)
        => new()
        {
            Id = game.Id,
            Title = game.Title,
            Summary = game.Summary,
            Content = game.Content,
            StartTimeUTC = game.StartTimeUTC,
            EndTimeUTC = game.EndTimeUTC
        };
}
