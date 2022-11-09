using CTFServer.Models.Request.Info;

namespace CTFServer.Models.Request.Game;

/// <summary>
/// 比赛 Writeup 提交信息
/// </summary>
public class BasicWriteupInfoModel
{
    /// <summary>
    /// 是否已经提交
    /// </summary>
    public bool Submitted { get; set; } = false;

    /// <summary>
    /// 文件名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 文件上传时间
    /// </summary>
    public DateTimeOffset UploadTimeUTC { get; set; } = DateTimeOffset.UtcNow;

    internal static BasicWriteupInfoModel FromParticipation(Participation part)
        => new()
        {
            Submitted = part.WriteUp is not null,
            Name = part.WriteUp?.Name ?? "#",
            UploadTimeUTC = part.WriteUp?.UploadTimeUTC ?? DateTimeOffset.UtcNow
        };
}