using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Data;

/// <summary>A sample document provided by the organizers, separate from team submissions.</summary>
public class WriteupExample
{
    [Key]
    public int Id { get; set; }
    public int GameId { get; set; }
    public Game Game { get; set; } = null!;
    public int FileId { get; set; }
    public LocalFile File { get; set; } = null!;

    // Keep the original name per example: a shared blob can be renamed by another upload.
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
}
