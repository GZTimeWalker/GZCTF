using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Request.Teams;

public class TeamUpdateModel
{
    /// <summary>
    /// 队伍名称
    /// </summary>
    [MaxLength(32)]
    public string? Name { get; set; } = string.Empty;

    /// <summary>
    /// 队伍签名
    /// </summary>
    public string? Bio { get; set; }
}