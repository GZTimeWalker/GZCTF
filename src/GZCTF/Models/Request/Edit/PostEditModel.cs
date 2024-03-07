﻿using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// 文章对象（Edit）
/// </summary>
public class PostEditModel
{
    /// <summary>
    /// 通知标题
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_TitleRequired))]
    [MaxLength(50, ErrorMessageResourceName = nameof(Resources.Program.Model_TitleTooLong))]
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
    /// 文章标签
    /// </summary>
    public string[] Tags { get; set; } = [];

    /// <summary>
    /// 是否置顶
    /// </summary>
    public bool IsPinned { get; set; } = false;
}
