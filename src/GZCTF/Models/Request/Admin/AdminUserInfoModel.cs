using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Admin;

/// <summary>
/// User information modification (Admin)
/// </summary>
public class AdminUserInfoModel
{
    /// <summary>
    /// Username
    /// </summary>
    [MinLength(Limits.MinUserNameLength, ErrorMessageResourceName = nameof(Resources.Program.Model_UserNameTooShort),
        ErrorMessageResourceType = typeof(Resources.Program))]
    [MaxLength(Limits.MaxUserNameLength, ErrorMessageResourceName = nameof(Resources.Program.Model_UserNameTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? UserName { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    [EmailAddress(ErrorMessageResourceName = nameof(Resources.Program.Model_EmailMalformed),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? Email { get; set; }

    /// <summary>
    /// Signature
    /// </summary>
    [MaxLength(Limits.MaxUserDataLength, ErrorMessageResourceName = nameof(Resources.Program.Model_BioTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? Bio { get; set; }

    /// <summary>
    /// Phone number
    /// </summary>
    [Phone(ErrorMessageResourceName = nameof(Resources.Program.Model_MalformedPhoneNumber),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? Phone { get; set; }

    /// <summary>
    /// Real name
    /// </summary>
    [MaxLength(Limits.MaxUserDataLength, ErrorMessageResourceName = nameof(Resources.Program.Model_RealNameTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? RealName { get; set; }

    /// <summary>
    /// Student number
    /// </summary>
    [MaxLength(Limits.MaxStdNumberLength, ErrorMessageResourceName = nameof(Resources.Program.Model_StdNumberTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? StdNumber { get; set; }

    /// <summary>
    /// Is email confirmed (can log in)
    /// </summary>
    public bool? EmailConfirmed { get; set; }

    /// <summary>
    /// User role
    /// </summary>
    public Role? Role { get; set; }
}
