using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Admin;

/// <summary>Request to batch-send credential emails to imported users.</summary>
public class SendCredentialsRequest
{
    [Required]
    public List<CredentialItem> Items { get; set; } = [];
}

/// <summary>A single user entry to send a "set your password" email to.</summary>
public class CredentialItem
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string UserName { get; set; } = string.Empty;
}
