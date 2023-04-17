﻿using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Request.Account;

/// <summary>
/// 登录
/// </summary>
public class LoginModel
{
    /// <summary>
    /// 用户名或邮箱
    /// </summary>
    [Required]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Required]
    [MinLength(6, ErrorMessage = "密码过短")]
    public string Password { get; set; } = string.Empty;
}