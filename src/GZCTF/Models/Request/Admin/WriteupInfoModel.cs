using System.Text.Json.Serialization;
using GZCTF.Models.Request.Info;

namespace GZCTF.Models.Request.Admin;

/// <summary>
/// Game writeup information
/// </summary>
public class WriteupInfoModel
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
    /// Writeup file object
    /// </summary>
    [JsonIgnore]
    public LocalFile File { get; set; } = null!;

    internal static WriteupInfoModel? FromParticipation(Participation part) =>
        part.Writeup is null
            ? null
            : new()
            {
                Id = part.Id,
                Team = TeamInfoModel.FromTeam(part.Team, false),
                File = part.Writeup,
                Url = part.Writeup.Url(),
                UploadTimeUtc = part.Writeup.UploadTimeUtc
            };
}
