using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Request.Edit;

/// <summary>
/// 全局通知（Edit）
/// </summary>
public class NoticeModel
{
    /// <summary>
    /// 通知标题
    /// </summary>
    [Required(ErrorMessage = "标题是必需的")]
    [MaxLength(50, ErrorMessage = "标题过长")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 通知内容
    /// </summary>
    [Required(ErrorMessage = "内容是必需的")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 是否置顶
    /// </summary>
    public bool IsPinned { get; set; } = false;
}