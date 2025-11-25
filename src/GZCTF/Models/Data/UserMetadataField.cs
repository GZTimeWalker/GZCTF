using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GZCTF.Models.Internal;

namespace GZCTF.Models.Data;

/// <summary>
/// User metadata field configuration
/// </summary>
public class UserMetadataField
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
    [MaxLength(Limits.MaxUserMetadataKeyLength)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the field
    /// </summary>
    [Required]
    [MaxLength(Limits.MaxDisplayNameLength)]
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
    /// Whether this field can only be edited by privileged flows (admin/provider)
    /// </summary>
    public bool Locked { get; set; }

    /// <summary>
    /// Placeholder text for the field
    /// </summary>
    [MaxLength(Limits.MaxUserMetadataPlaceholderLength)]
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
    [MaxLength(Limits.MaxRegexPatternLength)]
    public string? Pattern { get; set; }

    /// <summary>
    /// Options for select fields (stored as JSON)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public List<string>? Options { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int Order { get; set; }

    internal Internal.UserMetadataField ToField() => new()
    {
        Key = Key,
        DisplayName = DisplayName,
        Type = Type,
        Required = Required,
        Visible = Visible,
        Locked = Locked,
        Placeholder = Placeholder,
        MaxLength = MaxLength,
        MinValue = MinValue,
        MaxValue = MaxValue,
        Pattern = Pattern,
        Options = Options
    };

    internal void UpdateFromField(Internal.UserMetadataField field)
    {
        Key = field.Key;
        DisplayName = field.DisplayName;
        Type = field.Type;
        Required = field.Required;
        Visible = field.Visible;
        Locked = field.Locked;
        Placeholder = field.Placeholder;
        MaxLength = field.MaxLength;
        MinValue = field.MinValue;
        MaxValue = field.MaxValue;
        Pattern = field.Pattern;
        Options = field.Options;
    }
}
