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
    [Required(ErrorMessage = "用户名是必需的")]
    [MinLength(3, ErrorMessage = "用户名过短")]
    [MaxLength(15, ErrorMessage = "用户名过长")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Required(ErrorMessage = "密码是必需的")]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    [Required(ErrorMessage = "邮箱是必需的")]
    [EmailAddress(ErrorMessage = "邮箱地址无效")]
    public string Email { get; set; } = string.Empty;
}