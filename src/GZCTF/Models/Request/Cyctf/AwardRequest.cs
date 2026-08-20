namespace GZCTF.Models.Request.Cyctf;

/// <summary>
/// 创建/更新奖项请求
/// </summary>
public class AwardRequest
{
    /// <summary>
    /// 奖项名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 奖项描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 主色调
    /// </summary>
    public string? PrimaryColor { get; set; }

    /// <summary>
    /// 次色调
    /// </summary>
    public string? SecondaryColor { get; set; }

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; } = 0;
}
