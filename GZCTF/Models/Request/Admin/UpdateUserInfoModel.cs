using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Request.Admin;

/// <summary>
/// 用户信息更改（Admin）
/// </summary>
public class UpdateUserInfoModel
{
    /// <summary>
    /// 用户名
    /// </summary>
    [MinLength(5, ErrorMessage = "用户名过短")]
    [MaxLength(10, ErrorMessage = "用户名过长")]
    public string? UserName { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [EmailAddress(ErrorMessage = "邮箱格式错误")]
    public string? Email { get; set; }

    /// <summary>
    /// 签名
    /// </summary>
    [MaxLength(50, ErrorMessage = "描述过长")]
    public string? Bio { get; set; }

    /// <summary>
    /// 手机号码
    /// </summary>
    [Phone(ErrorMessage = "手机号格式错误")]
    public string? Phone { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    [MaxLength(6, ErrorMessage = "真实姓名过长")]
    public string? RealName { get; set; }

    /// <summary>
    /// 学工号
    /// </summary>
    [MaxLength(10, ErrorMessage = "学工号过长")]
    public string? StdNumber { get; set; }

    /// <summary>
    /// 用户是否通过邮箱验证（可登录）
    /// </summary>
    public bool? EmailConfirmed { get; set; }

    /// <summary>
    /// 用户角色
    /// </summary>
    public Role? Role { get; set; }
}