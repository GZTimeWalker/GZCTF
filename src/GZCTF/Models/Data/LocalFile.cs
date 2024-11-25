using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(Hash))]
public class LocalFile
{
    [Key]
    [JsonIgnore]
    public int Id { get; set; }

    /// <summary>
    /// File hash
    /// </summary>
    [MaxLength(Limits.FileHashLength)]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Upload time
    /// </summary>
    [JsonIgnore]
    public DateTimeOffset UploadTimeUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// File size
    /// </summary>
    [JsonIgnore]
    public long FileSize { get; set; }

    /// <summary>
    /// File name
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Reference count
    /// </summary>
    [JsonIgnore]
    public uint ReferenceCount { get; set; } = 1;

    /// <summary>
    /// File storage location
    /// </summary>
    [NotMapped]
    [JsonIgnore]
    public string Location => $"{Hash[..2]}/{Hash[2..4]}";

    /// <summary>
    /// URL for fetching the file
    /// </summary>
    public string Url(string? filename = null) => $"/assets/{Hash}/{filename ?? Name}";
}
