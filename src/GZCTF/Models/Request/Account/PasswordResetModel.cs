using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Account;

/// <summary>
/// Account password reset
/// </summary>
public class PasswordResetModel
{
    /// <summary>
    /// Password
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_PasswordRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Email
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_EmailRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Base64 formatted token received via email
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_TokenRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? RToken { get; set; }
}
