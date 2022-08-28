using CTFServer.Models.Request.Teams;

namespace CTFServer.Models.Request.Admin;

/// <summary>
/// 比赛参与对象，用于审核查看（Admin）
/// </summary>
public class ParticipationInfoModel
{
    /// <summary>
    /// 参与对象 Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 队伍分值
    /// </summary>
    public int Score { get; set; } = 0;

    /// <summary>
    /// 参与队伍
    /// </summary>
    public TeamWithDetailedUserInfo Team { get; set; } = default!;

    /// <summary>
    /// 参赛所属组织
    /// </summary>
    public string? Organization;

    /// <summary>
    /// 参与状态
    /// </summary>
    public ParticipationStatus Status { get; set; } = ParticipationStatus.Pending;

    internal static ParticipationInfoModel FromParticipation(Participation part)
        => new()
        {
            Id = part.Id,
            Score = part.Score,
            Status = part.Status,
            Organization = part.Organization,
            Team = TeamWithDetailedUserInfo.FromTeam(part.Team)
        };
}