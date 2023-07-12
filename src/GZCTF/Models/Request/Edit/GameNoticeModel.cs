using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// 比赛通知（Edit）
/// </summary>
public class GameNoticeModel
{
    /// <summary>
    /// 通知内容
    /// </summary>
    [Required(ErrorMessage = "内容是必需的")]
    public string Content { get; set; } = string.Empty;
}
