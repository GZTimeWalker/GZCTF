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
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_ContentRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Content { get; set; } = string.Empty;
}
