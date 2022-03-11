using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models;

public class LocalFile
{
    /// <summary>
    /// 文件哈希
    /// </summary>
    [Key]
    [MaxLength(64)]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// 文件名
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 文件存储位置
    /// </summary>
    [Required]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// 获取文件Url
    /// </summary>
    /// <returns>文件Url</returns>
    public string GetUrl() => throw new NotImplementedException();
}

