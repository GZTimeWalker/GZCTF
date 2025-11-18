using GZCTF.Models.Internal;

namespace GZCTF.Models.Request.Admin;

/// <summary>
/// OAuth provider configuration model for admin
/// </summary>
public class OAuthProviderEditModel
{
    /// <summary>
    /// Provider key (google, github, microsoft, etc.)
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Provider configuration
    /// </summary>
    public OAuthProviderConfig Config { get; set; } = new();
}

/// <summary>
/// User metadata field configuration model for admin
/// </summary>
public class UserMetadataFieldEditModel
{
    /// <summary>
    /// Metadata fields configuration
    /// </summary>
    public List<UserMetadataField> Fields { get; set; } = [];
}

/// <summary>
/// OAuth configuration model for admin
/// </summary>
public class OAuthConfigEditModel
{
    /// <summary>
    /// OAuth providers configuration
    /// </summary>
    public Dictionary<string, OAuthProviderConfig>? Providers { get; set; }

    /// <summary>
    /// User metadata fields configuration
    /// </summary>
    public List<UserMetadataField>? UserMetadataFields { get; set; }

    /// <summary>
    /// Whether to allow users to link multiple OAuth accounts
    /// </summary>
    public bool? AllowMultipleProviders { get; set; }
}
