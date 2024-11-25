using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Info;

/// <summary>
/// Team information update
/// </summary>
public class TeamUpdateModel
{
    /// <summary>
    /// Team name
    /// </summary>
    [MaxLength(Limits.MaxTeamNameLength, ErrorMessageResourceName = nameof(Resources.Program.Model_TeamNameTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? Name { get; set; } = string.Empty;

    /// <summary>
    /// Team bio
    /// </summary>
    [MaxLength(Limits.MaxTeamBioLength, ErrorMessageResourceName = nameof(Resources.Program.Model_TeamBioTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? Bio { get; set; }
}
