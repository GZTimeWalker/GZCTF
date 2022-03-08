using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models.Request;

/// <summary>
/// 答案提交
/// </summary>
public class AnswerSubmitModel
{
    /// <summary>
    /// 谜题答案
    /// </summary>
    [Required(ErrorMessage = "答案是必需的")]
    [MinLength(6, ErrorMessage = "答案过短")]
    [MaxLength(50, ErrorMessage = "答案过长")]
    [RegularExpression("^msc{[a-zA-Z0-9_-]+}$", ErrorMessage = "答案格式错误")]
    public string Answer { get; set; } = string.Empty;
}
