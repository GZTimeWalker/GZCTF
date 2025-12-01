using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Account;

/// <summary>
/// Passkey attestation completion request
/// </summary>
public class PasskeyAttestationRequest
{
    /// <summary>
    /// The credential JSON from navigator.credentials.create()
    /// </summary>
    [Required]
    public string CredentialJson { get; set; } = string.Empty;

    /// <summary>
    /// Optional passkey name for easier identification
    /// </summary>
    [MaxLength(64)]
    public string? Name { get; set; }
}

/// <summary>
/// Passkey assertion options request for login
/// </summary>
public class PasskeyAssertionOptionsRequest
{
    /// <summary>
    /// Optional username to scope allowed credentials
    /// </summary>
    public string? UserName { get; set; }
}

/// <summary>
/// Passkey assertion request for login
/// </summary>
public class PasskeyAssertionRequest
{
    /// <summary>
    /// The credential JSON from navigator.credentials.get()
    /// </summary>
    [Required]
    public string CredentialJson { get; set; } = string.Empty;
}

/// <summary>
/// Response model for passkey info
/// </summary>
public class PasskeyInfoModel
{
    /// <summary>
    /// The credential ID encoded as Base64URL
    /// </summary>
    public string CredentialId { get; set; } = string.Empty;

    /// <summary>
    /// User-friendly name for the passkey
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// When the passkey was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Whether the passkey is backed up
    /// </summary>
    public bool IsBackedUp { get; set; }

    /// <summary>
    /// Transport hints (e.g., "usb", "nfc", "ble", "internal")
    /// </summary>
    public string[]? Transports { get; set; }
}
