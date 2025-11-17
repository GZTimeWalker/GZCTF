using System.Text.Json.Serialization;
using GZCTF.Models.Request.Info;

namespace GZCTF.Models.Request.Admin;

/// <summary>
/// Game writeup information
/// </summary>
public record WriteupInfoModel
{
    /// <summary>
    /// Division ID to Division Name mapping
    /// </summary>
    public Dictionary<int, string> Divisions { get; set; } = [];

    /// <summary>
    /// Writeups list
    /// </summary>
    public List<WriteupInfo> Writeups { get; set; } = [];
}

public record WriteupInfo
{
    /// <summary>
    /// Participation ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Team information
    /// </summary>
    public TeamInfoModel Team { get; set; } = null!;

    /// <summary>
    /// File URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// File upload time
    /// </summary>
    public DateTimeOffset UploadTimeUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The division the team belongs to
    /// </summary>
    public int? DivisionId { get; set; }

    /// <summary>
    /// Writeup file object
    /// </summary>
    [JsonIgnore]
    public LocalFile File { get; set; } = null!;
}