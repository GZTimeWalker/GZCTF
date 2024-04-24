using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Admin;

/// <summary>
/// 用户信息更改（Admin）
/// </summary>
public class AdminUserInfoModel
{
    /// <summary>
    /// 用户名
    /// </summary>
    [MinLength(Limits.MinUserNameLength, ErrorMessageResourceName = nameof(Resources.Program.Model_UserNameTooShort),
        ErrorMessageResourceType = typeof(Resources.Program))]
    [MaxLength(Limits.MaxUserNameLength, ErrorMessageResourceName = nameof(Resources.Program.Model_UserNameTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? UserName { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [EmailAddress(ErrorMessageResourceName = nameof(Resources.Program.Model_EmailMalformed),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? Email { get; set; }

    /// <summary>
    /// 签名
    /// </summary>
    [MaxLength(Limits.MaxUserDataLength, ErrorMessageResourceName = nameof(Resources.Program.Model_BioTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? Bio { get; set; }

    /// <summary>
    /// 手机号码
    /// </summary>
    [Phone(ErrorMessageResourceName = nameof(Resources.Program.Model_MalformedPhoneNumber),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? Phone { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    [MaxLength(Limits.MaxUserDataLength, ErrorMessageResourceName = nameof(Resources.Program.Model_RealNameTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? RealName { get; set; }

    /// <summary>
    /// 学工号
    /// </summary>
    [MaxLength(Limits.MaxStdNumberLength, ErrorMessageResourceName = nameof(Resources.Program.Model_StdNumberTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
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
