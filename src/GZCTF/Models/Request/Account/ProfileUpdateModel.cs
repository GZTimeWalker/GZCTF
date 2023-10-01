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
    [MinLength(3, ErrorMessageResourceName = nameof(Resources.Program.Model_UserNameTooShort), ErrorMessageResourceType = typeof(Resources.Program))]
    [MaxLength(15, ErrorMessageResourceName = nameof(Resources.Program.Model_UserNameTooLong), ErrorMessageResourceType = typeof(Resources.Program))]
    public string? UserName { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [MaxLength(55, ErrorMessageResourceName = nameof(Resources.Program.Model_BioTooLong), ErrorMessageResourceType = typeof(Resources.Program))]
    public string? Bio { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    [Phone(ErrorMessageResourceName = nameof(Resources.Program.Model_MalformedPhoneNumber), ErrorMessageResourceType = typeof(Resources.Program))]
    public string? Phone { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    [MaxLength(7, ErrorMessageResourceName = nameof(Resources.Program.Model_RealNameTooLong), ErrorMessageResourceType = typeof(Resources.Program))]
    public string? RealName { get; set; }

    /// <summary>
    /// 学工号
    /// </summary>
    [MaxLength(24, ErrorMessageResourceName = nameof(Resources.Program.Model_StdNumberTooLong), ErrorMessageResourceType = typeof(Resources.Program))]
    public string? StdNumber { get; set; }
}