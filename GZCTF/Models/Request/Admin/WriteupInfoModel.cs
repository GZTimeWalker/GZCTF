using CTFServer.Models.Request.Info;

namespace CTFServer.Models.Request.Admin;

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
    public DateTimeOffset UploadTimeUTC { get; set; } = DateTimeOffset.UtcNow;

    internal static WriteupInfoModel FromParticipation(Participation part)
        => new()
        {
            Id = part.Id,
            Team = TeamInfoModel.FromTeam(part.Team, false),
            Url = part.WriteUp?.Url() ?? "#",
            UploadTimeUTC = part.WriteUp?.UploadTimeUTC ?? DateTimeOffset.UtcNow
        };
}