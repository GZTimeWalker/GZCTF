namespace CTFServer.Models.Request.Teams;

public class BasicUserInfoModel
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 签名
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// 头像链接
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 是否是队长
    /// </summary>
    public bool Captain { get; set; }
}
