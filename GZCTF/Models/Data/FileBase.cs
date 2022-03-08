using System.ComponentModel.DataAnnotations;

namespace CTFServer.Models;

public class FileBase
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Location { get; set; } = string.Empty;
}

