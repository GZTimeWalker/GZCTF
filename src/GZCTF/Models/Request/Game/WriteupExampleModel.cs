using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Game;

/// <summary>A downloadable writeup sample document.</summary>
public class WriteupExampleModel
{
    [Required]
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public long FileSize { get; set; }
    [Required]
    public string Url { get; set; } = string.Empty;

    internal static WriteupExampleModel FromExample(WriteupExample example) => new()
    {
        Id = example.Id,
        Name = example.Name,
        FileSize = example.File.FileSize,
        Url = example.File.Url(Uri.EscapeDataString(example.Name))
    };
}
