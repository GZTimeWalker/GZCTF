using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GZCTF.Models.Data;

public class Attachment
{
    [Key]
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Attachment type
    /// </summary>
    [Required]
    public FileType Type { get; set; } = FileType.None;

    /// <summary>
    /// Flag corresponding attachment (remote file)
    /// </summary>
    [JsonIgnore]
    public string? RemoteUrl { get; set; } = string.Empty;

    /// <summary>
    /// Local file ID
    /// </summary>
    [JsonIgnore]
    public int? LocalFileId { get; set; }

    /// <summary>
    /// Flag corresponding file (local file)
    /// </summary>
    [JsonIgnore]
    public LocalFile? LocalFile { get; set; }

    /// <summary>
    /// Default file URL
    /// </summary>
    [NotMapped]
    public string? Url => UrlWithName();

    /// <summary>
    /// Get attachment size
    /// </summary>
    [NotMapped]
    public long? FileSize => LocalFile?.FileSize;

    /// <summary>
    /// Attachment access link
    /// </summary>
    public string? UrlWithName(string? filename = null) =>
        Type switch
        {
            FileType.None => null,
            FileType.Local => LocalFile?.Url(filename),
            FileType.Remote => RemoteUrl,
            _ => throw new ArgumentException(nameof(Type))
        };
}
