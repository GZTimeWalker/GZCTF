using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Account;

/// <summary>
/// Account verification
/// </summary>
public class AccountVerifyModel
{
    /// <summary>
    /// Base64 formatted token received via email
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_TokenRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? Token { get; set; }

    /// <summary>
    /// Base64 formatted user email
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_EmailRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? Email { get; set; }
}
