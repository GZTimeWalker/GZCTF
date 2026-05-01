using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using GZCTF.Extensions;
using MemoryPack;

namespace GZCTF.Models.Data;

/// <summary>
/// User metadata field configuration
/// </summary>
[MemoryPackable]
public sealed partial class UserMetadataField : IDisposable
{
    /// <summary>
    /// Field key (e.g., "department", "studentId", "organization")
    /// </summary>
    [Key]
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
    /// Whether this field is hidden from user view (frontend only)
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    /// Whether this field can only be edited by privileged flows (admin/provider)
    /// </summary>
    public bool Locked { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Default value for the field (stored as JSON)
    /// </summary>
    [JsonDocumentFormatter]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonDocument? DefaultValue { get; set; }

    /// <summary>
    /// Maximum length for text fields
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxLength { get; set; }

    /// <summary>
    /// Minimum value for number fields
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MinValue { get; set; }

    /// <summary>
    /// Maximum value for number fields
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxValue { get; set; }

    /// <summary>
    /// Options for select fields (stored as JSON)
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SortedSet<string>? Options { get; set; }

    /// <summary>
    /// Validation pattern (regex) for the field
    /// </summary>
    [MaxLength(Limits.MaxRegexPatternLength)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Pattern { get; set; }

    /// <summary>
    /// Compiled regex pattern
    /// </summary>
    [NotMapped]
    [JsonIgnore]
    [MemoryPackIgnore]
    public Regex? CompiledPattern => string.IsNullOrWhiteSpace(Pattern) ? null : RegexHelper.GetCompiledRegex(Pattern);

    public UserMetadataFieldValue GetFieldValue(MetadataStore? fields)
    {
        if (fields is null || !fields.TryGetValue(Key, out var value) || value is null)
            return UserMetadataFieldValue.Null;

        var expectedType = Type.GetFieldValueType();
        var valueKind = value.RootElement.ValueKind;
        var isValidType = expectedType switch
        {
            UserMetadataFieldValueType.String => valueKind == JsonValueKind.String,
            UserMetadataFieldValueType.DateTime => valueKind == JsonValueKind.Number,
            UserMetadataFieldValueType.Number => valueKind == JsonValueKind.Number,
            UserMetadataFieldValueType.Boolean => valueKind is JsonValueKind.True or JsonValueKind.False,
            UserMetadataFieldValueType.StringArray => valueKind == JsonValueKind.Array,
            _ => false,
        };

        return isValidType ? new UserMetadataFieldValue(expectedType, value) : UserMetadataFieldValue.Null;
    }

    public void Dispose() => DefaultValue?.Dispose();
}
