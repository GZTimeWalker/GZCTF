using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Request.Edit;

/// <summary>
/// flag 提交
/// </summary>
public class FlagSubmitModel
{
    /// <summary>
    /// flag 内容
    /// fix: 防止前端的意外提交 (number/float/null) 可能被错误转换
    /// </summary>
    [Required(ErrorMessage = "flag 是必需的")]
    [MaxLength(126, ErrorMessage = "flag 过长")]
    public string Flag { get; set; } = string.Empty;
}