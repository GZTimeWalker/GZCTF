﻿using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Account;

/// <summary>
/// 密码更改
/// </summary>
public class PasswordChangeModel
{
    /// <summary>
    /// 旧密码
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_OldPasswordRequired))]
    [MinLength(6, ErrorMessageResourceName = nameof(Resources.Program.Model_OldPasswordTooShort))]
    public string Old { get; set; } = string.Empty;

    /// <summary>
    /// 新密码
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_NewPasswordRequired))]
    [MinLength(6, ErrorMessageResourceName = nameof(Resources.Program.Model_NewPasswordTooShort))]
    public string New { get; set; } = string.Empty;
}
