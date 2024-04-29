using System.Text.Json.Serialization;

namespace GZCTF.Models.Request.Game;

/// <summary>
/// 比赛 Writeup 提交信息
/// </summary>
public class BasicWriteupInfoModel
{
    /// <summary>
    /// 是否已经提交
    /// </summary>
    public bool Submitted { get; set; }

    /// <summary>
    /// 文件名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Writeup 附加说明
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
