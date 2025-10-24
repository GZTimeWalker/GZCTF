﻿using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Account;

/// <summary>
/// Basic account information
/// </summary>
public class ProfileUserInfoModel
{
    /// <summary>
    /// User ID
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// User role
    /// </summary>
    [Required]
    public Role Role { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Bio
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// Phone number
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Real name
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// Student ID
    /// </summary>
    public string? StdNumber { get; set; }

    /// <summary>
    /// Avatar URL
    /// </summary>
    public string? Avatar { get; set; }

    internal static ProfileUserInfoModel FromUserInfo(UserInfo user) =>
        new()
        {
            UserId = user.Id,
            Bio = user.Bio,
            Email = user.Email,
            UserName = user.UserName,
            RealName = user.RealName,
            Phone = user.PhoneNumber,
            Avatar = user.AvatarUrl,
            StdNumber = user.StdNumber,
            Role = user.Role
        };
}
