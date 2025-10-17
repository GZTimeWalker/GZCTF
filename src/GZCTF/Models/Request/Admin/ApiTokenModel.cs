using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Admin;

/// <summary>
/// API token creation model.
/// </summary>
public class ApiTokenCreateModel
{
    /// <summary>
    /// The user-friendly name for the token to identify its purpose.
    /// </summary>
    [Required]
    [MaxLength(128)]
    public required string Name { get; set; }

    /// <summary>
    /// The duration for which the token will be valid, in days.
    /// </summary>
    public uint? ExpiresIn { get; set; }
}

/// <summary>
/// This record represents the response for an API token request.
/// </summary>
public record ApiTokenResponse(string Token, ApiToken Info);
