namespace GZCTF.Models.Request.Cyctf;

/// <summary>
/// 创建/更新赞助商请求
/// </summary>
public class SponsorRequest
{
    /// <summary>
    /// 赞助商简称
    /// </summary>
    public string ShortName { get; set; } = string.Empty;

    /// <summary>
    /// 赞助商全称
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// 赞助商网站
    /// </summary>
    public string? Website { get; set; }

    /// <summary>
    /// Logo URL 或文件路径
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// 赞助商类型
    /// </summary>
    public string Type { get; set; } = "SPONSOR";

    /// <summary>
    /// 类型标签
    /// </summary>
    public string? TypeLabel { get; set; }

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; } = 0;
}
