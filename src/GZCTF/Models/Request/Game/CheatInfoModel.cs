namespace GZCTF.Models.Request.Game;

/// <summary>
/// Cheat behavior information
/// </summary>
public class CheatInfoModel
{
    /// <summary>
    /// Team owning the flag
    /// </summary>
    public ParticipationModel OwnedTeam { get; set; } = default!;

    /// <summary>
    /// Team submitting the flag
    /// </summary>
    public ParticipationModel SubmitTeam { get; set; } = default!;

    /// <summary>
    /// Submission corresponding to this cheating behavior
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
