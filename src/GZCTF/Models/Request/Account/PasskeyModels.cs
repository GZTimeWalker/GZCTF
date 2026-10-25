using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Account;

/// <summary>
/// Public metadata for a registered passkey. Credential material is never returned.
/// </summary>
public class PasskeyModel
{
    /// <summary>
    /// Base64url-encoded credential ID
    /// </summary>
    public required string CredentialId { get; set; }

    /// <summary>
    /// User-assigned passkey name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Creation time
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Current password confirmation for passkey registration
/// </summary>
public class PasskeyCreationModel
{
    /// <summary>
    /// Current password, encrypted when API encryption is enabled
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_PasswordRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// WebAuthn credential returned by the browser
/// </summary>
public class PasskeyCredentialModel
{
    /// <summary>
    /// Serialized public-key credential with base64url-encoded binary fields
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_PasskeyCredentialRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    [MaxLength(Limits.MaxPasskeyCredentialLength,
        ErrorMessageResourceName = nameof(Resources.Program.Model_PasskeyCredentialTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string CredentialJson { get; set; } = string.Empty;
}

/// <summary>
/// A new passkey and its user-assigned name
/// </summary>
public class PasskeyRegistrationModel : PasskeyCredentialModel
{
    /// <summary>
    /// Passkey name
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_PasskeyNameRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    [MaxLength(Limits.MaxPasskeyNameLength,
        ErrorMessageResourceName = nameof(Resources.Program.Model_PasskeyNameTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Update a passkey's display name
/// </summary>
public class PasskeyRenameModel
{
    /// <summary>
    /// New passkey name
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_PasskeyNameRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    [MaxLength(Limits.MaxPasskeyNameLength,
        ErrorMessageResourceName = nameof(Resources.Program.Model_PasskeyNameTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Request token for passkey operations
/// </summary>
public class PasskeyAntiforgeryModel
{
    /// <summary>
    /// Send in the X-CSRF-TOKEN header together with the matching antiforgery cookie
    /// </summary>
    public required string Token { get; set; }
}
