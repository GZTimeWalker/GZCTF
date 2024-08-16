namespace GZCTF.Models.Request.Game;

public class GameJoinModel
{
    /// <summary>
    /// 参赛队伍 Id
    /// </summary>
    public int TeamId { get; set; }

    /// <summary>
    /// 参赛单位
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// 参赛邀请码
    /// </summary>
    public string? InviteCode { get; set; }
}
