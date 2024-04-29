using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GZCTF.Models.Request.Game;

/// <summary>
/// 比赛详细信息，包含详细介绍与当前队伍报名状态
/// </summary>
public class DetailedGameInfoModel
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
    /// 是否为隐藏比赛
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    /// 参赛所属单位列表
    /// </summary>
    public HashSet<string>? Organizations { get; set; }

    /// <summary>
    /// 是否需要邀请码
    /// </summary>
    public bool InviteCodeRequired { get; set; }

    /// <summary>
    /// 是否需要提交 Writeup
    /// </summary>
    public bool WriteupRequired { get; set; }

    /// <summary>
    /// 比赛头图
    /// </summary>
    [JsonPropertyName("poster")]
    public string? PosterUrl { get; set; } = string.Empty;

    /// <summary>
    /// 队员数量限制
    /// </summary>
    [JsonPropertyName("limit")]
    public int TeamMemberCountLimit { get; set; }

    /// <summary>
    /// 报名参赛队伍数量
    /// </summary>
    public int TeamCount { get; set; }

    /// <summary>
    /// 当前报名的组织
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// 参赛队伍名称
    /// </summary>
    public string? TeamName { get; set; }

    /// <summary>
    /// 比赛是否为练习模式（比赛结束够依然可以访问）
    /// </summary>
    public bool PracticeMode { get; set; } = true;

    /// <summary>
    /// 队伍参与状态
    /// </summary>
    [JsonPropertyName("status")]
    public ParticipationStatus Status { get; set; } = ParticipationStatus.Unsubmitted;

    /// <summary>
    /// 开始时间
    /// </summary>
    [JsonPropertyName("start")]
    public DateTimeOffset StartTimeUtc { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    /// <summary>
    /// 结束时间
    /// </summary>
    [JsonPropertyName("end")]
    public DateTimeOffset EndTimeUtc { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    public DetailedGameInfoModel WithParticipation(Participation? part)
    {
        Status = part?.Status ?? ParticipationStatus.Unsubmitted;
        TeamName = part?.Team.Name;
        Organization = part?.Organization;
        return this;
    }

    internal static DetailedGameInfoModel FromGame(Data.Game game, int count) =>
        new()
        {
            Id = game.Id,
            Title = game.Title,
            Hidden = game.Hidden,
            Summary = game.Summary,
            Content = game.Content,
            PracticeMode = game.PracticeMode,
            Organizations = game.Organizations,
            InviteCodeRequired = !string.IsNullOrWhiteSpace(game.InviteCode),
            WriteupRequired = game.WriteupRequired,
            TeamCount = count,
            PosterUrl = game.PosterUrl,
            StartTimeUtc = game.StartTimeUtc,
            EndTimeUtc = game.EndTimeUtc,
            TeamMemberCountLimit = game.TeamMemberCountLimit
        };
}
