using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Account;

/// <summary>
/// 邮箱更改
/// </summary>
public class MailChangeModel
{
    /// <summary>
    /// 新邮箱
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_EmailRequired))]
    [EmailAddress(ErrorMessageResourceName = nameof(Resources.Program.Model_EmailMalformed))]
    public string NewMail { get; set; } = string.Empty;
}
