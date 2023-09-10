﻿using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Admin;

/// <summary>
/// 批量用户创建（Admin）
/// </summary>
public class UserCreateModel
{
    /// <summary>
    /// 用户名
    /// </summary>
    [Required(ErrorMessage = "用户名是必需的")]
    [MinLength(3, ErrorMessage = "用户名过短")]
    [MaxLength(15, ErrorMessage = "用户名过长")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Required(ErrorMessage = "密码是必需的")]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    [Required(ErrorMessage = "邮箱是必需的")]
    [EmailAddress(ErrorMessage = "邮箱地址无效")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 真实姓名
    /// </summary>
    [MaxLength(7, ErrorMessage = "真实姓名过长")]
    public string? RealName { get; set; }

    /// <summary>
    /// 学号
    /// </summary>
    [MaxLength(15, ErrorMessage = "学工号过长")]
    public string? StdNumber { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    [Phone(ErrorMessage = "手机号格式错误")]
    public string? Phone { get; set; }

    /// <summary>
    /// 用户加入的队伍
    /// </summary>
    [MaxLength(15, ErrorMessage = "队伍名称过长")]
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