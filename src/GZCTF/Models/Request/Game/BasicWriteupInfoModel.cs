using System.Text.Json.Serialization;

namespace GZCTF.Models.Request.Game;

/// <summary>
/// Game writeup submission information
/// </summary>
public class BasicWriteupInfoModel
{
    /// <summary>
    /// Whether it has been submitted
    /// </summary>
    public bool Submitted { get; set; }

    /// <summary>
    /// File name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// File size
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Writeup additional notes
    /// </summary>
    [JsonPropertyName("note")]
    public string WriteupNote { get; set; } = string.Empty;

    internal static BasicWriteupInfoModel FromParticipation(Participation part) =>
        new()
        {
            Submitted = part.Writeup is not null,
            Name = part.Writeup?.Name ?? "#",
            FileSize = part.Writeup?.FileSize ?? 0,
            WriteupNote = part.Game.WriteupNote
        };
}
