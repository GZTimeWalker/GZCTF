namespace GZCTF.Models.Request.Admin;

/// <summary>
/// Modify the participation information
/// </summary>
public class ParticipationEditModel
{
    public ParticipationEditModel() { }

    public ParticipationEditModel(ParticipationStatus status)
    {
        Status = status;
    }

    /// <summary>
    /// Participation Status
    /// </summary>
    public ParticipationStatus? Status { get; set; }

    /// <summary>
    /// The division of the participated team
    /// </summary>
    public string? Division { get; set; }
}
