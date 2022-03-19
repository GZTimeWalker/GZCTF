using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTFServer.Models;

public class LocalFile
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 文件哈希
    /// </summary>
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
    [NotMapped]
    public string Location => $"{Hash[..2]}/{Hash[2..4]}";

    /// <summary>
    /// 获取文件Url
    /// </summary>
    /// <returns>文件Url</returns>
    public string GetUrl() => throw new NotImplementedException();
}

