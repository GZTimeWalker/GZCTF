using System.Text.Json.Serialization;
using GZCTF.Models.Request.Info;

namespace GZCTF.Models.Request.Admin;

/// <summary>
/// 比赛 Writeup 信息
/// </summary>
public class WriteupInfoModel
{
    /// <summary>
    /// 参与对象 Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 队伍信息
    /// </summary>
    public TeamInfoModel Team { get; set; } = default!;

    /// <summary>
    /// 文件链接
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 文件上传时间
    /// </summary>
    public DateTimeOffset UploadTimeUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Writeup 文件对象
    /// </summary>
    [JsonIgnore]
    public LocalFile File { get; set; } = default!;

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
