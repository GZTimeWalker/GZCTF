using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Account;

/// <summary>
/// Public metadata for a registered passkey. Credential material is never returned.
/// </summary>
public class PasskeyModel
{
    public required string CredentialId { get; set; }
    public string? Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class PasskeyCreationModel
{
    [Required]
    public string Password { get; set; } = string.Empty;
}

public class PasskeyCredentialModel
{
    [Required, MaxLength(65536)]
    public string CredentialJson { get; set; } = string.Empty;
}

public class PasskeyRegistrationModel : PasskeyCredentialModel
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}

public class PasskeyRenameModel
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}

public class PasskeyAntiforgeryModel
{
    public required string Token { get; set; }
}
