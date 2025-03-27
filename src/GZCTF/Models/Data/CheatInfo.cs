using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// Record of cheating behavior
/// </summary>
[Index(nameof(GameId))]
[Index(nameof(SubmissionId), IsUnique = true)]
public class CheatInfo
{
    #region Db Relationship

    /// <summary>
    /// Game object
    /// </summary>
    public Game Game { get; set; } = null!;

    /// <summary>
    /// Game object ID
    /// </summary>
    public int GameId { get; set; }

    /// <summary>
    /// Team that submitted the corresponding flag
    /// </summary>
    public Participation SubmitTeam { get; set; } = null!;

    /// <summary>
    /// Team ID that submitted the corresponding flag
    /// </summary>
    public int SubmitTeamId { get; set; }

    /// <summary>
    /// Team that owns the corresponding flag
    /// </summary>
    public Participation SourceTeam { get; set; } = null!;

    /// <summary>
    /// Team ID that owns the corresponding flag
    /// </summary>
    public int SourceTeamId { get; set; }

    /// <summary>
    /// Submission corresponding to this cheating behavior
    /// </summary>
    public Submission Submission { get; set; } = null!;

    /// <summary>
    /// Submission ID corresponding to this cheating behavior
    /// </summary>
    public int SubmissionId { get; set; }

    #endregion Db Relationship
}
