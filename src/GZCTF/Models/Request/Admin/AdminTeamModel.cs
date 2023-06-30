namespace CTFServer.Models.Request.Admin;

/// <summary>
/// 队伍信息更改（Admin）
/// </summary>
public class AdminTeamModel
{
    /// <summary>
    /// 队伍名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 队伍签名
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// 是否锁定
    /// </summary>
    public bool? Locked { get; set; }
}