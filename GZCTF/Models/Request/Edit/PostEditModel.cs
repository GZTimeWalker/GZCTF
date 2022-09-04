using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Request.Edit;

/// <summary>
/// 文章对象（Edit）
/// </summary>
public class PostEditModel
{
    /// <summary>
    /// 通知标题
    /// </summary>
    [Required(ErrorMessage = "标题是必需的")]
    [MaxLength(50, ErrorMessage = "标题过长")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 文章总结
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 文章内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 是否置顶
    /// </summary>
    public bool IsPinned { get; set; } = false;
}