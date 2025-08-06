using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// Represents an API token for programmatic access.
/// </summary>
[Table("ApiTokens")]
[Comment("Stores API tokens for programmatic access.")]
public class ApiToken
{
    /// <summary>
    /// The unique identifier for the token, also used as the JWT ID (jti).
    /// </summary>
    [Key]
    [Comment("The unique identifier for the token.")]
    public Guid Id { get; set; }

    /// <summary>
    /// A user-friendly name for the token to identify its purpose.
    /// </summary>
    [Required]
    [MaxLength(128)]
    [Comment("A user-friendly name for the token.")]
    public required string Name { get; set; }

    /// <summary>
    /// The ID of the user who created the token.
    /// </summary>
    [Required]
    [Comment("The ID of the user who created the token.")]
    public Guid? CreatorId { get; set; }

    /// <summary>
    /// Navigation property for the user who created the token.
    /// </summary>
    [JsonIgnore]
    [ForeignKey(nameof(CreatorId))]
    public UserInfo? Creator { get; set; }

    /// <summary>
    /// The timestamp when the token was created.
    /// </summary>
    [Required]
    [Comment("The timestamp when the token was created.")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The timestamp when the token expires. A null value means it never expires.
    /// </summary>
    [Comment("The timestamp when the token expires. A null value means it never expires.")]
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// The timestamp when the token was last used.
    /// </summary>
    [Comment("The timestamp when the token was last used.")]
    public DateTimeOffset? LastUsedAt { get; set; }

    /// <summary>
    /// Indicates whether the token has been revoked.
    /// </summary>
    [Required]
    [Comment("Indicates whether the token has been revoked.")]
    public bool IsRevoked { get; set; }

    /// <summary>
    /// The name of the user who created the token.
    /// </summary>
    [NotMapped]
    [JsonPropertyName("creator")]
    public string? CreatorName => Creator?.UserName;
}
