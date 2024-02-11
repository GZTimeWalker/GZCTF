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
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_EmailRequired))]
    [EmailAddress(ErrorMessageResourceName = nameof(Resources.Program.Model_EmailMalformed))]
    public string? Email { get; set; }
}
