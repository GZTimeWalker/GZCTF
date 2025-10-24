namespace GZCTF.Models.Request.Admin;

/// <summary>
/// Modify the participation information
/// </summary>
public class ParticipationEditModel
{
    /// <summary>
    /// Participation Status
    /// </summary>
    public ParticipationStatus? Status { get; set; }

    /// <summary>
    /// The division of the participated team
    /// </summary>
    public int? DivisionId { get; set; }
}
