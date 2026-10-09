using System.Text.Json.Serialization;

namespace GZCTF.Models.Request.Account;

/// <summary>
/// Basic account information
/// </summary>
public class ProfileUserInfoModel
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User role
    /// </summary>
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
    /// Avatar URL
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// User metadata (custom fields)
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MetadataStore? Metadata { get; set; }

    internal static ProfileUserInfoModel FromUserInfo(UserInfo user) =>
        new()
        {
            UserId = user.Id,
            Bio = user.Bio,
            Email = user.Email,
            UserName = user.UserName,
            Phone = user.PhoneNumber,
            Avatar = user.AvatarUrl,
            Metadata = user.Metadata,
            Role = user.Role
        };
}
