using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Account;

/// <summary>
/// Basic account information update
/// </summary>
public class ProfileUpdateModel
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
    /// Description
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
    /// Student ID
    /// </summary>
    [MaxLength(Limits.MaxStdNumberLength, ErrorMessageResourceName = nameof(Resources.Program.Model_StdNumberTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? StdNumber { get; set; }
}
