using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Request.Teams;

/// <summary>
/// 队伍信息更新
/// </summary>
public class TeamUpdateModel
{
    /// <summary>
    /// 队伍名称
    /// </summary>
    [MaxLength(16, ErrorMessage = "队伍名称过长")]
    [RegularExpression("[0-9A-Za-z]+", ErrorMessage = "队名名称只能使用数字和字母")]
    public string? Name { get; set; } = string.Empty;

    /// <summary>
    /// 队伍签名
    /// </summary>
    [MaxLength(32, ErrorMessage = "队伍签名过长")]
    public string? Bio { get; set; }
}