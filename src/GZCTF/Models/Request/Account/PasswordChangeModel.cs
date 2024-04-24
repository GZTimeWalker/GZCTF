using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Account;

/// <summary>
/// 密码更改
/// </summary>
public class PasswordChangeModel
{
    /// <summary>
    /// 旧密码
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_OldPasswordRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    [MinLength(Limits.MinPasswordLength, ErrorMessageResourceName = nameof(Resources.Program.Model_OldPasswordTooShort),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Old { get; set; } = string.Empty;

    /// <summary>
    /// 新密码
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_NewPasswordRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    [MinLength(Limits.MinPasswordLength, ErrorMessageResourceName = nameof(Resources.Program.Model_NewPasswordTooShort),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string New { get; set; } = string.Empty;
}
