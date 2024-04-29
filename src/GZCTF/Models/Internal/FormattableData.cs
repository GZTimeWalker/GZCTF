using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Internal;

/// <summary>
/// 格式化数据
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class FormattableData<T> where T : Enum
{
    /// <summary>
    /// 数据类型
    /// </summary>
    [Required]
    public T Type { get; set; } = default!;

    /// <summary>
    /// 格式化值列表
    /// </summary>
    [Required]
    public List<string>? Values { get; set; } = [];
}
