using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Models;

[Index(nameof(ParticipationId))]
public class UserParticipation
{
    public UserParticipation() { }

    public UserParticipation(UserInfo user, Game game, Team team)
    {
        User = user;
        Game = game;
        Team = team;
    }

    /// <summary>
    /// 参赛用户 Id
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 参赛用户
    /// </summary>
    public UserInfo User { get; set; } = default!;

    /// <summary>
    /// 参赛队伍 Id
    /// </summary>
    [Required]
    public int TeamId { get; set; }

    /// <summary>
    /// 参赛队伍
    /// </summary>
    public Team Team { get; set; } = default!;

    /// <summary>
    /// 比赛 Id
    /// </summary>
    [Required]
    public int GameId { get; set; }

    /// <summary>
    /// 比赛
    /// </summary>
    public Game Game { get; set; } = default!;

    /// <summary>
    /// 参与对象 Id
    /// </summary>
    [Required]
    public int ParticipationId { get; set; }

    /// <summary>
    /// 参赛对象
    /// </summary>
    public Participation Participation = default!;
}
