using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Game;

/// <summary>
/// Flag submission
/// </summary>
public class FlagSubmitModel
{
    /// <summary>
    /// Flag content
    /// Fix: Prevent accidental submissions from the frontend (number/float/null) that may be incorrectly converted
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_FlagRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    [MaxLength(Limits.MaxFlagLength, ErrorMessageResourceName = nameof(Resources.Program.Model_FlagTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Flag { get; set; } = string.Empty;
}
