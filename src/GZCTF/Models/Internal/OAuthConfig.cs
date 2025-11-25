using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GZCTF.Models.Internal;

/// <summary>
/// User metadata field type
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<UserMetadataFieldType>))]
public enum UserMetadataFieldType
{
    /// <summary>
    /// Single-line text input
    /// </summary>
    Text,

    /// <summary>
    /// Multi-line text input
    /// </summary>
    TextArea,

    /// <summary>
    /// Number input
    /// </summary>
    Number,

    /// <summary>
    /// Email input
    /// </summary>
    Email,

    /// <summary>
    /// URL input
    /// </summary>
    Url,

    /// <summary>
    /// Phone number input
    /// </summary>
    Phone,

    /// <summary>
    /// Date input
    /// </summary>
    Date,

    /// <summary>
    /// Dropdown select
    /// </summary>
    Select
}

/// <summary>
/// User metadata field configuration
/// </summary>
public class UserMetadataField
{
    /// <summary>
    /// Field key (e.g., "department", "studentId", "organization")
    /// </summary>
    [Required]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the field
    /// </summary>
    [Required]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Field type
    /// </summary>
    public UserMetadataFieldType Type { get; set; } = UserMetadataFieldType.Text;

    /// <summary>
    /// Whether this field is required
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Whether this field is visible to users
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Whether the field is locked for direct user edits
    /// </summary>
    public bool Locked { get; set; }

    /// <summary>
    /// Placeholder text for the field
    /// </summary>
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
    public string? Pattern { get; set; }

    /// <summary>
    /// Options for select fields
    /// </summary>
    public List<string>? Options { get; set; }
}

/// <summary>
/// OAuth provider configuration
/// </summary>
public class OAuthProviderConfig
{
    /// <summary>
    /// Whether this provider is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Client ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client Secret
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Authorization endpoint
    /// </summary>
    [Required]
    public string AuthorizationEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Token endpoint
    /// </summary>
    [Required]
    public string TokenEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// User information endpoint
    /// </summary>
    [Required]
    public string UserInformationEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the provider
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Scopes to request
    /// </summary>
    public List<string> Scopes { get; set; } = [];

    /// <summary>
    /// Field mapping from OAuth provider fields to user metadata fields
    /// Key: OAuth provider field name (e.g., "email", "name", "avatar_url")
    /// Value: User metadata field key
    /// </summary>
    public Dictionary<string, string> FieldMapping { get; set; } = new();
}
