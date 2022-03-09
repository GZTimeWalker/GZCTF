using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models;

public class FileBase
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

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
}

