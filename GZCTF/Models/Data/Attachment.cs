using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTFServer.Models.Data;

public class Attachment
{
    [Key]
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// 附件类型
    /// </summary>
    [Required]
    public FileType Type { get; set; } = FileType.None;

    /// <summary>
    /// Flag 对应附件 (远程文件）
    /// </summary>
    [JsonIgnore]
    public string? RemoteUrl { get; set; } = string.Empty;

    /// <summary>
    /// 本地文件 Id
    /// </summary>
    [JsonIgnore]
    public int? LocalFileId { get; set; }

    /// <summary>
    /// Flag 对应文件（本地文件）
    /// </summary>
    [JsonIgnore]
    public LocalFile? LocalFile { get; set; } = default;

    /// <summary>
    /// 附件访问链接
    /// </summary>
    [NotMapped]
    public string? Url => Type switch
    {
        FileType.None => null,
        FileType.Local => LocalFile?.Url,
        FileType.Remote => RemoteUrl,
        _ => throw new ArgumentException(nameof(Type))
    };
}