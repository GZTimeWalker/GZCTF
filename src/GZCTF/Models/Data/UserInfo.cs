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
    public override Guid Id { get; set; } = Ulid.NewUlid().ToGuid();

    /// <summary>
    /// 用户角色
    /// </summary>
    [ProtectedPersonalData]
    public Role Role { get; set; } = Role.User;

    /// <summary>
    /// 用户最近访问IP
    /// </summary>
    [MaxLength(Limits.MaxIPLength)]
    [ProtectedPersonalData]
    public string IP { get; set; } = "0.0.0.0";

    /// <summary>
    /// 用户最近登录时间
    /// </summary>
    public DateTimeOffset LastSignedInUtc { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    /// <summary>
    /// 用户最近访问时间
    /// </summary>
    public DateTimeOffset LastVisitedUtc { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    /// <summary>
    /// 用户注册时间
    /// </summary>
    public DateTimeOffset RegisterTimeUtc { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    /// <summary>
    /// 个性签名
    /// </summary>
    [MaxLength(Limits.MaxUserDataLength)]
    public string Bio { get; set; } = string.Empty;

    /// <summary>
    /// 真实姓名
    /// </summary>
    [MaxLength(Limits.MaxUserDataLength)]
    [ProtectedPersonalData]
    public string RealName { get; set; } = string.Empty;

    /// <summary>
    /// 学工号
    /// </summary>
    [MaxLength(Limits.MaxStdNumberLength)]
    [ProtectedPersonalData]
    public string StdNumber { get; set; } = string.Empty;

    /// <summary>
    /// 在练习排行榜中隐藏
    /// </summary>
    public bool ExerciseVisible { get; set; } = true;

    [NotMapped]
    [MemoryPackIgnore]
    public string? AvatarUrl => AvatarHash is null ? null : $"/assets/{AvatarHash}/avatar";

    /// <summary>
    /// 通过Http请求更新用户最新访问时间和IP
    /// </summary>
    /// <param name="context"></param>
    public void UpdateByHttpContext(HttpContext context)
    {
        LastVisitedUtc = DateTimeOffset.UtcNow;

        IPAddress? remoteAddress = context.Connection.RemoteIpAddress;

        if (remoteAddress is null)
            return;

        IP = remoteAddress.ToString();
    }

    internal void UpdateUserInfo(AdminUserInfoModel model)
    {
        // use SetUserNameAsync and SetEmailAsync to update UserName and Email
        Bio = model.Bio ?? Bio;
        Role = model.Role ?? Role;
        StdNumber = model.StdNumber ?? StdNumber;
        RealName = model.RealName ?? RealName;
        PhoneNumber = model.Phone ?? PhoneNumber;
        EmailConfirmed = model.EmailConfirmed ?? EmailConfirmed;
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
        StdNumber = model.StdNumber ?? StdNumber;
        RealName = model.RealName ?? RealName;
        PhoneNumber = model.Phone ?? PhoneNumber;
    }

    internal void UpdateUserInfo(ProfileUpdateModel model)
    {
        // use SetUserNameAsync to update UserName
        Bio = model.Bio ?? Bio;
        PhoneNumber = model.Phone ?? PhoneNumber;
        RealName = model.RealName ?? RealName;
        StdNumber = model.StdNumber ?? StdNumber;
    }

    #region Db Relationship

    /// <summary>
    /// 头像哈希
    /// </summary>
    [MaxLength(Limits.FileHashLength)]
    public string? AvatarHash { get; set; }

    /// <summary>
    /// 个人提交记录
    /// </summary>
    [MemoryPackIgnore]
    public List<Submission> Submissions { get; set; } = [];

    /// <summary>
    /// 参与的队伍
    /// </summary>
    [MemoryPackIgnore]
    public List<Team> Teams { get; set; } = [];

    #endregion
}
