using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(RuleCode), IsUnique = true)]
public class SuspicionRule
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string RuleCode { get; set; } = string.Empty;

    public int Weight { get; set; }

    public string Description { get; set; } = string.Empty;
}
