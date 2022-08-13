using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Request.Account;

/// <summary>
/// 找回账号
/// </summary>
public class RecoveryModel
{
    /// <summary>
    /// 用户邮箱
    /// </summary>
    [Required(ErrorMessage = "邮箱是必需的")]
    [EmailAddress(ErrorMessage = "邮箱地址无效")]
    public string? Email { get; set; }

    /// <summary>
    /// Google Recaptcha Token
    /// </summary>
    public string? GToken { get; set; }
}