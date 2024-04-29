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
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_EmailRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    [EmailAddress(ErrorMessageResourceName = nameof(Resources.Program.Model_EmailMalformed),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string NewMail { get; set; } = string.Empty;
}
