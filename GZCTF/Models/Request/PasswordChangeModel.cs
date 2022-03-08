using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Request;

/// <summary>
/// 密码更改
/// </summary>
public class PasswordChangeModel
{
    /// <summary>
    /// 旧密码
    /// </summary>
    [Required(ErrorMessage = "旧密码是必需的")]
    [MinLength(6, ErrorMessage = "旧密码过短")]
    public string? Old { get; set; }

    /// <summary>
    /// 新密码
    /// </summary>
    [Required(ErrorMessage = "新密码是必需的")]
    [MinLength(6, ErrorMessage = "新密码过短")]
    public string? New { get; set; }
}
