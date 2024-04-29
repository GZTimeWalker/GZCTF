using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Account;

/// <summary>
/// 账号验证
/// </summary>
public class AccountVerifyModel
{
    /// <summary>
    /// 邮箱接收到的Base64格式Token
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_TokenRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? Token { get; set; }

    /// <summary>
    /// 用户邮箱的Base64格式
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_EmailRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? Email { get; set; }
}
