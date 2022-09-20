using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTFServer.Models.Data;

public class Webhook
{
    [Key, Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public required string TargetUrl { get; set; }
    public required string Scope { get; set; }
    public required string Event { get; set; }
    [MaxLength(5)]
    public string Method { get; set; } = "POST";
    public required string Body { get; set; }
    public bool Enabled { get; set; }
}
