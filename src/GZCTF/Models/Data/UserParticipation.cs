using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(ParticipationId))]
[Index(nameof(UserId), nameof(GameId), IsUnique = true)]
public class UserParticipation
{
    /// <summary>
    /// Participation object
    /// </summary>
    public Participation Participation = null!;

    public UserParticipation() { }

    public UserParticipation(UserInfo user, Game game, Team team)
    {
        User = user;
        Game = game;
        Team = team;
    }

    #region Db Relationship

    /// <summary>
    /// User ID
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// User
    /// </summary>
    public UserInfo User { get; set; } = null!;

    /// <summary>
    /// Team ID
    /// </summary>
    [Required]
    public int TeamId { get; set; }

    /// <summary>
    /// Team
    /// </summary>
    public Team Team { get; set; } = null!;

    /// <summary>
    /// Game ID
    /// </summary>
    [Required]
    public int GameId { get; set; }

    /// <summary>
    /// Game
    /// </summary>
    public Game Game { get; set; } = null!;

    /// <summary>
    /// Participation ID
    /// </summary>
    [Required]
    public int ParticipationId { get; set; }

    #endregion
}
