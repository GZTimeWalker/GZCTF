using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Info;

/// <summary>
/// 队伍信息更新
/// </summary>
public class TeamUpdateModel
{
    public TeamUpdateModel() { }

    public TeamUpdateModel(string teamName)
    {
        Name = teamName;
    }

    /// <summary>
    /// 队伍名称
    /// </summary>
    [MaxLength(15, ErrorMessageResourceName = nameof(Resources.Program.Model_TeamNameTooLong), ErrorMessageResourceType = typeof(Resources.Program))]
    public string? Name { get; set; } = string.Empty;

    /// <summary>
    /// 队伍签名
    /// </summary>
    [MaxLength(31, ErrorMessageResourceName = nameof(Resources.Program.Model_TeamBioTooLong), ErrorMessageResourceType = typeof(Resources.Program))]
    public string? Bio { get; set; }
}