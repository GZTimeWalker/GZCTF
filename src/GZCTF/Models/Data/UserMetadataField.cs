using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using MemoryPack;

namespace GZCTF.Models.Data;

/// <summary>
/// User metadata field configuration
/// </summary>
[MemoryPackable]
public partial class UserMetadataField
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
    /// Whether this field is visible to users
    /// </summary>
    [DefaultValue(true)]
    public bool Visible { get; set; } = true;

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
    [Column(TypeName = "jsonb")]
    [JsonDocumentFormatter]
    public JsonDocument? DefaultValue { get; set; }

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
    /// Options for select fields (stored as JSON)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public SortedSet<string>? Options { get; set; }

    /// <summary>
    /// Validation pattern (regex) for the field
    /// </summary>
    [MaxLength(Limits.MaxRegexPatternLength)]
    public string? Pattern { get; set; }

    /// <summary>
    /// Compiled regex pattern
    /// </summary>
    [NotMapped]
    [JsonIgnore]
    [MemoryPackIgnore]
    public Regex? CompiledPattern => string.IsNullOrWhiteSpace(Pattern) ? null : RegexHelper.GetCompiledRegex(Pattern);
}

