using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Data;

public class LogModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    public DateTimeOffset TimeUtc { get; set; }

    [Required]
    [MaxLength(Limits.MaxLogLevelLength)]
    public string Level { get; set; } = string.Empty;

    [Required]
    [MaxLength(Limits.MaxLoggerLength)]
    public string Logger { get; set; } = string.Empty;

    [MaxLength(Limits.MaxLogStatusLength)]
    public string? Status { get; set; }

    [MaxLength(Limits.MaxIPLength)]
    public string? RemoteIP { get; set; }

    [MaxLength(Limits.MaxUserNameLength)]
    public string? UserName { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? Exception { get; set; }
}
