namespace GZCTF.Models.Request.Admin;

/// <summary>
/// 用户信息（Admin）
/// </summary>
public class UserInfoModel
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 学号
    /// </summary>
    public string? StdNumber { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 签名
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// 注册时间
    /// </summary>
    public DateTimeOffset RegisterTimeUtc { get; set; }

    /// <summary>
    /// 用户最近访问时间
    /// </summary>
    public DateTimeOffset LastVisitedUtc { get; set; }

    /// <summary>
    /// 用户最近访问IP
    /// </summary>
    public string IP { get; set; } = "0.0.0.0";

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 头像链接
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 用户角色
    /// </summary>
    public Role? Role { get; set; }

    /// <summary>
    /// 用户是否通过邮箱验证（可登录）
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
