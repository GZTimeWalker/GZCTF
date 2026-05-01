using System.Net;
using System.Text.Json.Serialization;

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
    public IPAddress IP { get; set; } = IPAddress.Any;

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

    /// <summary>
    /// User metadata (custom fields)
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MetadataStore? Metadata { get; set; }

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
            UserName = user.UserName,
            Metadata = user.Metadata,
            LastVisitedUtc = user.LastVisitedUtc,
            RegisterTimeUtc = user.RegisterTimeUtc,
            EmailConfirmed = user.EmailConfirmed
        };
}
