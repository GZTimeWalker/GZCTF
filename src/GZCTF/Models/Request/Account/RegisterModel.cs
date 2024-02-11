using System.ComponentModel.DataAnnotations;
using GZCTF.Extensions;

namespace GZCTF.Models.Request.Account;

/// <summary>
/// 注册账号
/// </summary>
public class RegisterModel : ModelWithCaptcha
{
    /// <summary>
    /// 用户名
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_UserNameRequired))]
    [MinLength(3, ErrorMessageResourceName = nameof(Resources.Program.Model_UserNameTooShort))]
    [MaxLength(15, ErrorMessageResourceName = nameof(Resources.Program.Model_UserNameTooLong))]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_PasswordRequired))]
    [MinLength(6, ErrorMessageResourceName = nameof(Resources.Program.Model_PasswordTooShort))]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_EmailRequired))]
    [EmailAddress(ErrorMessageResourceName = nameof(Resources.Program.Model_EmailMalformed))]
    public string Email { get; set; } = string.Empty;
}
