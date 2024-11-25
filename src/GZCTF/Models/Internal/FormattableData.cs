using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Internal;

/// <summary>
/// Formattable data
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class FormattableData<T> where T : Enum
{
    /// <summary>
    /// Data type
    /// </summary>
    [Required]
    public T Type { get; set; } = default!;

    /// <summary>
    /// List of formatted values
    /// </summary>
    [Required]
    public List<string>? Values { get; set; } = [];
}
