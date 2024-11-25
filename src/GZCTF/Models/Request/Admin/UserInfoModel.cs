namespace GZCTF.Models.Request.Admin;

/// <summary>
/// User information (Admin)
/// </summary>
public class UserInfoModel
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Real name
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// Student number
    /// </summary>
    public string? StdNumber { get; set; }

    /// <summary>
    /// Contact phone number
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Bio
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// Registration time
    /// </summary>
    public DateTimeOffset RegisterTimeUtc { get; set; }

    /// <summary>
    /// Last visit time
    /// </summary>
    public DateTimeOffset LastVisitedUtc { get; set; }

    /// <summary>
    /// Last visit IP
    /// </summary>
    public string IP { get; set; } = "0.0.0.0";

    /// <summary>
    /// Email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Avatar URL
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// User role
    /// </summary>
    public Role? Role { get; set; }

    /// <summary>
    /// Is email confirmed (can log in)
    /// </summary>
    public bool? EmailConfirmed { get; set; }

    internal static UserInfoModel FromUserInfo(UserInfo user) =>
        new()
        {
            Id = user.Id,
            IP = user.IP,
            Bio = user.Bio,
            Role = user.Role,
            Email = user.Email,
            Phone = user.PhoneNumber,
            Avatar = user.AvatarUrl,
            RealName = user.RealName,
            UserName = user.UserName,
            StdNumber = user.StdNumber,
            LastVisitedUtc = user.LastVisitedUtc,
            RegisterTimeUtc = user.RegisterTimeUtc,
            EmailConfirmed = user.EmailConfirmed
        };
}
