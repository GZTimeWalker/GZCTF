using System;
namespace CTFServer.Models.Request.Game;

public class GameJoinModel
{
    /// <summary>
    /// 参赛单位
    /// </summary>
    public string? Organization;

    /// <summary>
    /// 参赛邀请码
    /// </summary>
    public string? InviteCode;
}

