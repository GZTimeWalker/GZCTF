using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CTFServer.Models.Request.Game;

/// <summary>
/// 比赛详细信息，包含详细介绍与当前队伍报名状态
/// </summary>
public class GameDetailModel
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
    /// 参赛所属单位列表
    /// </summary>
    public List<string>? Organizations { get; set; }

    /// <summary>
    /// 是否需要邀请码
    /// </summary>
    public bool InviteCodeRequired { get; set; } = false;

    /// <summary>
    /// 比赛头图
    /// </summary>
    [JsonPropertyName("poster")]
    public string? PosterUrl { get; set; } = string.Empty;

    /// <summary>
    /// 队员数量限制
    /// </summary>
    [JsonPropertyName("limit")]
    public int TeamMemberCountLimit { get; set; } = 0;

    /// <summary>
    /// 报名参赛队伍数量
    /// </summary>
    public int TeamCount { get; set; } = 0;

    /// <summary>
    /// 当前报名的组织
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// 参赛队伍名称
    /// </summary>
    public string? TeamName { get; set; }

    /// <summary>
    /// 队伍参与状态
    /// </summary>
    [JsonPropertyName("status")]
    public ParticipationStatus Status { get; set; } = ParticipationStatus.Unsubmitted;

    /// <summary>
    /// 开始时间
    /// </summary>
    [JsonPropertyName("start")]
    public DateTimeOffset StartTimeUTC { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    /// <summary>
    /// 结束时间
    /// </summary>
    [JsonPropertyName("end")]
    public DateTimeOffset EndTimeUTC { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    public GameDetailModel WithParticipation(Participation? part)
    {
        Status = part?.Status ?? ParticipationStatus.Unsubmitted;
        TeamName = part?.Team.Name;
        Organization = part?.Organization;
        return this;
    }

    internal static GameDetailModel FromGame(Models.Game game, int count)
        => new()
        {
            Id = game.Id,
            Title = game.Title,
            Summary = game.Summary,
            Content = game.Content,
            Organizations = game.Organizations,
            InviteCodeRequired = !string.IsNullOrWhiteSpace(game.InviteCode),
            TeamCount = count,
            PosterUrl = game.PosterUrl,
            StartTimeUTC = game.StartTimeUTC,
            EndTimeUTC = game.EndTimeUTC,
            TeamMemberCountLimit = game.TeamMemberCountLimit
        };
}