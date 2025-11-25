namespace GZCTF.Models.Request.Account;

/// <summary>
/// Request payload for updating user metadata
/// </summary>
public class UserMetadataUpdateModel
{
    /// <summary>
    /// Metadata values keyed by configured field key
    /// </summary>
    public Dictionary<string, string?> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
