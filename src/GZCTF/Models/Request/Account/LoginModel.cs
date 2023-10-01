using System.ComponentModel.DataAnnotations;
using GZCTF.Extensions;

namespace GZCTF.Models.Request.Account;

/// <summary>
/// 登录
/// </summary>
public class LoginModel : ModelWithCaptcha
{
    /// <summary>
    /// 用户名或邮箱
    /// </summary>
    [Required]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Required]
    [MinLength(6, ErrorMessageResourceName = nameof(Resources.Program.Model_PasswordTooShort), ErrorMessageResourceType = typeof(Resources.Program))]
    public string Password { get; set; } = string.Empty;
}