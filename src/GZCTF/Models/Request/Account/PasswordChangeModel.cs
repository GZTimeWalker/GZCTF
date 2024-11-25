using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Account;

/// <summary>
/// Password change
/// </summary>
public class PasswordChangeModel
{
    /// <summary>
    /// Old password
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_OldPasswordRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    [MinLength(Limits.MinPasswordLength, ErrorMessageResourceName = nameof(Resources.Program.Model_OldPasswordTooShort),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Old { get; set; } = string.Empty;

    /// <summary>
    /// New password
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_NewPasswordRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    [MinLength(Limits.MinPasswordLength, ErrorMessageResourceName = nameof(Resources.Program.Model_NewPasswordTooShort),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string New { get; set; } = string.Empty;
}
