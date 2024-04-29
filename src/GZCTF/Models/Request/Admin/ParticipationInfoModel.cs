namespace GZCTF.Models.Request.Admin;

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
    /// 参与队伍
    /// </summary>
    public TeamWithDetailedUserInfo Team { get; set; } = default!;

    /// <summary>
    /// 注册的成员
    /// </summary>
    public Guid[] RegisteredMembers { get; set; } = [];

    /// <summary>
    /// 参赛所属组织
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// 参与状态
    /// </summary>
    public ParticipationStatus Status { get; set; } = ParticipationStatus.Pending;

    internal static ParticipationInfoModel FromParticipation(Participation part) =>
        new()
        {
            Id = part.Id,
            Status = part.Status,
            Organization = part.Organization,
            RegisteredMembers = part.Members.Select(m => m.UserId).ToArray(),
            Team = TeamWithDetailedUserInfo.FromTeam(part.Team)
        };
}
