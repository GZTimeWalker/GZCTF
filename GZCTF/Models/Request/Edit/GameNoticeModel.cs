using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Request.Edit;

public class GameNoticeModel
{
    /// <summary>
    /// 通知内容
    /// </summary>
    [Required(ErrorMessage = "内容是必需的")]
    public string Content { get; set; } = string.Empty;
}
