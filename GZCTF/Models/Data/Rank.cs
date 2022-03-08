using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models;

[Index(nameof(UserId), IsUnique = true)]
public class Rank
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 当前得分
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTimeOffset UpdateTimeUTC { get; set; }

    #region 数据库关系

    /// <summary>
    /// 用户数据库Id
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// 用户
    /// </summary>
    public UserInfo? User { get; set; }

    #endregion 数据库关系
}
