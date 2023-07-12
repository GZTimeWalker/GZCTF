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
    [Required(ErrorMessage = "Token是必需的")]
    public string? Token { get; set; }

    /// <summary>
    /// 用户邮箱的Base64格式
    /// </summary>
    [Required(ErrorMessage = "邮箱是必需的")]
    public string? Email { get; set; }
}
