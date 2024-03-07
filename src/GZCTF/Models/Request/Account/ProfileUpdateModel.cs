using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Account;

/// <summary>
/// 基本账号信息更改
/// </summary>
public class ProfileUpdateModel
{
    /// <summary>
    /// 用户名
    /// </summary>
    [MinLength(3, ErrorMessageResourceName = nameof(Resources.Program.Model_UserNameTooShort))]
    [MaxLength(15, ErrorMessageResourceName = nameof(Resources.Program.Model_UserNameTooLong))]
    public string? UserName { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [MaxLength(55, ErrorMessageResourceName = nameof(Resources.Program.Model_BioTooLong))]
    public string? Bio { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    [Phone(ErrorMessageResourceName = nameof(Resources.Program.Model_MalformedPhoneNumber))]
    public string? Phone { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    [MaxLength(7, ErrorMessageResourceName = nameof(Resources.Program.Model_RealNameTooLong))]
    public string? RealName { get; set; }

    /// <summary>
    /// 学工号
    /// </summary>
    [MaxLength(24, ErrorMessageResourceName = nameof(Resources.Program.Model_StdNumberTooLong))]
    public string? StdNumber { get; set; }
}
