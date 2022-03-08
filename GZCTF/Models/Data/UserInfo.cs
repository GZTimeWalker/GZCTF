using Microsoft.AspNetCore.Identity;

namespace CTFServer.Models;

public class UserInfo : IdentityUser
{
    /// <summary>
    /// 用户权限
    /// </summary>
    public Privilege Privilege { get; set; } = Privilege.User;

    /// <summary>
    /// 用户最近访问IP
    /// </summary>
    public string IP { get; set; } = "0.0.0.0";

    /// <summary>
    /// 用户最近登录时间
    /// </summary>
    public DateTimeOffset LastSignedInUTC { get; set; } = DateTimeOffset.Parse("1970-01-01T00:00:00Z");

    /// <summary>
    /// 用户最近访问时间
    /// </summary>
    public DateTimeOffset LastVisitedUTC { get; set; } = DateTimeOffset.Parse("1970-01-01T00:00:00Z");

    /// <summary>
    /// 用户注册时间
    /// </summary>
    public DateTimeOffset RegisterTimeUTC { get; set; } = DateTimeOffset.Parse("1970-01-01T00:00:00Z");

    /// <summary>
    /// 个性签名
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 访问权限
    /// </summary>
    public int AccessLevel { get; set; } = 0;

    /// <summary>
    /// 学号
    /// </summary>
    public string StudentId { get; set; } = string.Empty;

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string RealName { get; set; } = string.Empty;

    #region 数据库关系

    public int RankId { get; set; }

    public Rank? Rank { get; set; }

    public List<Submission> Submissions { get; set; } = new();

    #endregion 数据库关系

    /// <summary>
    /// 通过Http请求更新用户最新访问时间和IP
    /// </summary>
    /// <param name="context"></param>
    public void UpdateByHttpContext(HttpContext context)
    {
        LastVisitedUTC = DateTimeOffset.UtcNow;

        var remoteAddress = context.Connection.RemoteIpAddress;

        if (remoteAddress is null)
            return;

        if (remoteAddress.IsIPv4MappedToIPv6)
            IP = remoteAddress.MapToIPv4().ToString();
        else
            IP = remoteAddress.MapToIPv6().ToString().Replace("::ffff:", "");

    }
}
