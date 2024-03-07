using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Admin;

/// <summary>
/// 队伍信息更改（Admin）
/// </summary>
public class AdminTeamModel
{
    /// <summary>
    /// 队伍名称
    /// </summary>
    [MaxLength(15, ErrorMessageResourceName = nameof(Resources.Program.Model_TeamNameTooLong))]
    public string? Name { get; set; } = string.Empty;

    /// <summary>
    /// 队伍签名
    /// </summary>
    [MaxLength(31, ErrorMessageResourceName = nameof(Resources.Program.Model_TeamBioTooLong))]
    public string? Bio { get; set; }

    /// <summary>
    /// 是否锁定
    /// </summary>
    public bool? Locked { get; set; }
}
