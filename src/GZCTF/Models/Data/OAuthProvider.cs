using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GZCTF.Models.Internal;

namespace GZCTF.Models.Data;

/// <summary>
/// OAuth provider configuration stored in database
/// </summary>
public class OAuthProvider
{
    /// <summary>
    /// Unique ID
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Provider key (google, github, microsoft, etc.)
    /// </summary>
    [Required]
    [MaxLength(Limits.MaxShortIdLength)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Whether this provider is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Client ID
    /// </summary>
    [MaxLength(Limits.MaxOAuthClientIdLength)]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client Secret (encrypted)
    /// </summary>
    [MaxLength(Limits.MaxOAuthClientSecretLength)]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Authorization endpoint
    /// </summary>
    [Required]
    [MaxLength(Limits.MaxUrlLength)]
    public string AuthorizationEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Token endpoint
    /// </summary>
    [Required]
    [MaxLength(Limits.MaxUrlLength)]
    public string TokenEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// User information endpoint
    /// </summary>
    [Required]
    [MaxLength(Limits.MaxUrlLength)]
    public string UserInformationEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the provider
    /// </summary>
    [MaxLength(Limits.MaxDisplayNameLength)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Scopes to request (stored as JSON)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public List<string> Scopes { get; set; } = [];

    /// <summary>
    /// Field mapping from OAuth provider fields to user metadata fields (stored as JSON)
    /// Key: OAuth provider field name, Value: User metadata field key
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, string> FieldMapping { get; set; } = new();

    /// <summary>
    /// Last updated time
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    internal OAuthProviderConfig ToConfig() => new()
    {
        Enabled = Enabled,
        ClientId = ClientId,
        ClientSecret = ClientSecret,
        AuthorizationEndpoint = AuthorizationEndpoint,
        TokenEndpoint = TokenEndpoint,
        UserInformationEndpoint = UserInformationEndpoint,
        DisplayName = DisplayName,
        Scopes = Scopes,
        FieldMapping = FieldMapping
    };

    internal void UpdateFromConfig(OAuthProviderConfig config)
    {
        Enabled = config.Enabled;
        ClientId = config.ClientId;
        ClientSecret = config.ClientSecret;
        AuthorizationEndpoint = config.AuthorizationEndpoint;
        TokenEndpoint = config.TokenEndpoint;
        UserInformationEndpoint = config.UserInformationEndpoint;
        DisplayName = config.DisplayName;
        Scopes = config.Scopes;
        FieldMapping = config.FieldMapping;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
