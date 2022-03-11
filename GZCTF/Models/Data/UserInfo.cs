using Microsoft.AspNetCore.Identity;

namespace CTFServer.Models;

public class UserInfo : IdentityUser
{
    /// <summary>
    /// 用户角色
    /// </summary>
    public Role Role { get; set; } = Role.User;

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
    public string Bio { get; set; } = string.Empty;

    #region 数据库关系
    /// <summary>
    /// 头像
    /// </summary>
    public LocalFile? Avatar { get; set; }

    /// <summary>
    /// 头像 Id
    /// </summary>
    public string AcatarId { get; set; } = string.Empty;

    /// <summary>
    /// 个人提交记录
    /// </summary>
    public List<Submission> Submissions { get; set; } = new();

    /// <summary>
    /// 当前激活队伍
    /// </summary>
    public Team? ActiveTeam { get; set; } 

    /// <summary>
    /// 当前激活队伍 Id
    /// </summary>
    public int ActiveTeamId { get; set; }

    /// <summary>
    /// 参与的队伍
    /// </summary>
    public List<Team> Teams { get; set; } = new();

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
