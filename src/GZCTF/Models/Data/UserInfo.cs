using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Admin;
using MemoryPack;
using Microsoft.AspNetCore.Identity;

namespace GZCTF.Models.Data;

[MemoryPackable]
public partial class UserInfo : IdentityUser<Guid>
{
    /// <summary>
    /// Override Guid to use Ulid
    /// </summary>
    [PersonalData]
    public override Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// User role
    /// </summary>
    [ProtectedPersonalData]
    public Role Role { get; set; } = Role.User;

    /// <summary>
    /// User's recent IP address
    /// </summary>
    [IPAddressFormatter]
    public IPAddress IP { get; set; } = IPAddress.Any;

    /// <summary>
    /// User's last sign-in time
    /// </summary>
    public DateTimeOffset LastSignedInUtc { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    /// <summary>
    /// User's last visit time
    /// </summary>
    public DateTimeOffset LastVisitedUtc { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    /// <summary>
    /// User registration time
    /// </summary>
    public DateTimeOffset RegisterTimeUtc { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    /// <summary>
    /// User bio
    /// </summary>
    [MaxLength(Limits.MaxUserDataLength)]
    public string Bio { get; set; } = string.Empty;

    /// <summary>
    /// Hide in exercise scoreboard
    /// </summary>
    public bool ExerciseVisible { get; set; } = true;

    /// <summary>
    /// User metadata stored as JSON (flexible user fields)
    /// </summary>
    [ProtectedPersonalData]
    public MetadataStore? Metadata { get; set; }

    [NotMapped]
    [MemoryPackIgnore]
    public string? AvatarUrl => AvatarHash is null ? null : $"/assets/{AvatarHash}/avatar";

    /// <summary>
    /// Update user's last visit time and IP address via HTTP request
    /// </summary>
    /// <param name="context">The HTTP context</param>
    public void UpdateByHttpContext(HttpContext context)
    {
        LastVisitedUtc = DateTimeOffset.UtcNow;

        var remoteAddress = context.Connection.RemoteIpAddress;

        if (remoteAddress is null)
            return;

        IP = remoteAddress;
    }

    internal void UpdateUserInfo(AdminUserInfoModel model)
    {
        // use SetUserNameAsync and SetEmailAsync to update UserName and Email
        Bio = model.Bio ?? Bio;
        Role = model.Role ?? Role;
        PhoneNumber = model.Phone ?? PhoneNumber;
        EmailConfirmed = model.EmailConfirmed ?? EmailConfirmed;
        Metadata = model.Metadata ?? Metadata;
    }

    /// <summary>
    /// Update user info by UserCreateModel
    /// for batch creation
    /// </summary>
    /// <param name="model"></param>
    internal void UpdateUserInfo(UserCreateModel model)
    {
        UserName = model.UserName;
        Email = model.Email;
        PhoneNumber = model.Phone ?? PhoneNumber;
        Metadata = model.Metadata ?? Metadata;
    }

    internal void UpdateUserInfo(ProfileUpdateModel model)
    {
        // use SetUserNameAsync to update UserName
        Bio = model.Bio ?? Bio;
        PhoneNumber = model.Phone ?? PhoneNumber;
        Metadata = model.Metadata ?? Metadata;
    }

    #region Db Relationship

    /// <summary>
    /// Avatar hash
    /// </summary>
    [MaxLength(Limits.FileHashLength)]
    public string? AvatarHash { get; set; }

    /// <summary>
    /// Personal submission records
    /// </summary>
    [MemoryPackIgnore]
    public List<Submission> Submissions { get; set; } = [];

    /// <summary>
    /// Participated teams
    /// </summary>
    [MemoryPackIgnore]
    public List<Team> Teams { get; set; } = [];

    #endregion
}
