using System.ComponentModel.DataAnnotations;
using GZCTF.Extensions;

namespace GZCTF.Models.Request.Account;

/// <summary>
/// 找回账号
/// </summary>
public class RecoveryModel : ModelWithCaptcha
{
    /// <summary>
    /// 用户邮箱
    /// </summary>
    [Required(ErrorMessage = "邮箱是必需的")]
    [EmailAddress(ErrorMessage = "邮箱地址无效")]
    public string? Email { get; set; }
}
