using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HttpMethod = System.Net.Http.HttpMethod;

namespace CTFServer.Models.Data;

public class Webhook
{
    [Key, Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public required string TargetUrl { get; set; }
    public required string Scope { get; set; }
    public required string Event { get; set; }
    public HttpMethod Method { get; set; } = HttpMethod.Post;
    public required string Body { get; set; }
    public bool Enabled { get; set; }
}
