using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Account;

/// <summary>
/// 账号密码重置
/// </summary>
public class PasswordResetModel
{
    /// <summary>
    /// 密码
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_PasswordRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_EmailRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱接收到的Base64格式Token
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_TokenRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? RToken { get; set; }
}
