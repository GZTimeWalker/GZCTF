namespace GZCTF.Models.Request.Game;

/// <summary>
/// 作弊行为信息
/// </summary>
public class CheatInfoModel
{
    /// <summary>
    /// flag 所属队伍
    /// </summary>
    public ParticipationModel OwnedTeam { get; set; } = default!;

    /// <summary>
    /// 提交对应 flag 的队伍
    /// </summary>
    public ParticipationModel SubmitTeam { get; set; } = default!;

    /// <summary>
    /// 本次抄袭行为对应的提交
    /// </summary>
    public Submission Submission { get; set; } = default!;

    internal static CheatInfoModel FromCheatInfo(CheatInfo info) =>
        new()
        {
            Submission = info.Submission,
            OwnedTeam = ParticipationModel.FromParticipation(info.SourceTeam),
            SubmitTeam = ParticipationModel.FromParticipation(info.SubmitTeam)
        };
}
