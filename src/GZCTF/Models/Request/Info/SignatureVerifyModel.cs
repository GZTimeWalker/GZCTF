using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Info;

/// <summary>
/// Signature verification
/// </summary>
public class SignatureVerifyModel
{
    /// <summary>
    /// Team token
    /// </summary>
    [Required]
    public string TeamToken { get; set; } = string.Empty;

    /// <summary>
    /// Game public key, Base64 encoded
    /// </summary>
    [Required]
    public string PublicKey { get; set; } = string.Empty;
}
