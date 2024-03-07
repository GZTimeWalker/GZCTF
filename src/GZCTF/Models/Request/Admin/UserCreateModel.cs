using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Admin;

/// <summary>
/// 批量用户创建（Admin）
/// </summary>
public class UserCreateModel
{
    /// <summary>
    /// 用户名
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_UserNameRequired))]
    [MinLength(3, ErrorMessageResourceName = nameof(Resources.Program.Model_UserNameTooShort))]
    [MaxLength(15, ErrorMessageResourceName = nameof(Resources.Program.Model_UserNameTooLong))]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_PasswordRequired))]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_EmailRequired))]
    [EmailAddress(ErrorMessageResourceName = nameof(Resources.Program.Model_EmailMalformed))]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 真实姓名
    /// </summary>
    [MaxLength(7, ErrorMessageResourceName = nameof(Resources.Program.Model_RealNameTooLong))]
    public string? RealName { get; set; }

    /// <summary>
    /// 学号
    /// </summary>
    [MaxLength(24, ErrorMessageResourceName = nameof(Resources.Program.Model_StdNumberTooLong))]
    public string? StdNumber { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    [Phone(ErrorMessageResourceName = nameof(Resources.Program.Model_MalformedPhoneNumber))]
    public string? Phone { get; set; }

    /// <summary>
    /// 用户加入的队伍
    /// </summary>
    [MaxLength(15, ErrorMessageResourceName = nameof(Resources.Program.Model_TeamNameTooLong))]
    public string? TeamName { get; set; }

    internal UserInfo ToUserInfo() =>
        new()
        {
            Email = Email,
            UserName = UserName,
            RealName = RealName ?? "",
            StdNumber = StdNumber ?? "",
            PhoneNumber = Phone,
            EmailConfirmed = true
        };
}
