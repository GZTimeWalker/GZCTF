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
    /// Provider key (google, github, microsoft, etc.)
    /// </summary>
    [Key]
    [MaxLength(50)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Provider type
    /// </summary>
    public OAuthProviderType Type { get; set; }

    /// <summary>
    /// Whether this provider is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Client ID
    /// </summary>
    [MaxLength(500)]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client Secret (encrypted)
    /// </summary>
    [MaxLength(1000)]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Authorization endpoint (for Generic provider)
    /// </summary>
    [MaxLength(500)]
    public string? AuthorizationEndpoint { get; set; }

    /// <summary>
    /// Token endpoint (for Generic provider)
    /// </summary>
    [MaxLength(500)]
    public string? TokenEndpoint { get; set; }

    /// <summary>
    /// User information endpoint (for Generic provider)
    /// </summary>
    [MaxLength(500)]
    public string? UserInformationEndpoint { get; set; }

    /// <summary>
    /// Display name for the provider
    /// </summary>
    [MaxLength(100)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Scopes to request (stored as JSON)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public List<string> Scopes { get; set; } = [];

    /// <summary>
    /// Last updated time
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    internal OAuthProviderConfig ToConfig() => new()
    {
        Type = Type,
        Enabled = Enabled,
        ClientId = ClientId,
        ClientSecret = ClientSecret,
        AuthorizationEndpoint = AuthorizationEndpoint,
        TokenEndpoint = TokenEndpoint,
        UserInformationEndpoint = UserInformationEndpoint,
        DisplayName = DisplayName,
        Scopes = Scopes
    };

    internal void UpdateFromConfig(OAuthProviderConfig config)
    {
        Type = config.Type;
        Enabled = config.Enabled;
        ClientId = config.ClientId;
        ClientSecret = config.ClientSecret;
        AuthorizationEndpoint = config.AuthorizationEndpoint;
        TokenEndpoint = config.TokenEndpoint;
        UserInformationEndpoint = config.UserInformationEndpoint;
        DisplayName = config.DisplayName;
        Scopes = config.Scopes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// User metadata field configuration
/// </summary>
public class UserMetadataFieldConfig
{
    /// <summary>
    /// Unique ID
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Field key (e.g., "department", "studentId", "organization")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the field
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Field type (text, number, email, etc.)
    /// </summary>
    [MaxLength(50)]
    public string Type { get; set; } = "text";

    /// <summary>
    /// Whether this field is required
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Whether this field is visible to users
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Placeholder text for the field
    /// </summary>
    [MaxLength(200)]
    public string? Placeholder { get; set; }

    /// <summary>
    /// Maximum length for text fields
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Minimum value for number fields
    /// </summary>
    public int? MinValue { get; set; }

    /// <summary>
    /// Maximum value for number fields
    /// </summary>
    public int? MaxValue { get; set; }

    /// <summary>
    /// Validation pattern (regex) for the field
    /// </summary>
    [MaxLength(500)]
    public string? Pattern { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int Order { get; set; }

    internal UserMetadataField ToField() => new()
    {
        Key = Key,
        DisplayName = DisplayName,
        Type = Type,
        Required = Required,
        Visible = Visible,
        Placeholder = Placeholder,
        MaxLength = MaxLength,
        MinValue = MinValue,
        MaxValue = MaxValue,
        Pattern = Pattern
    };

    internal void UpdateFromField(UserMetadataField field)
    {
        Key = field.Key;
        DisplayName = field.DisplayName;
        Type = field.Type;
        Required = field.Required;
        Visible = field.Visible;
        Placeholder = field.Placeholder;
        MaxLength = field.MaxLength;
        MinValue = field.MinValue;
        MaxValue = field.MaxValue;
        Pattern = field.Pattern;
    }
}
