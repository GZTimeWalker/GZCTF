using GZCTF.Models.Request.Game;

namespace GZCTF.Hubs.Clients;

/// <summary>
/// Client interface for the public AttackHub.
/// </summary>
public interface IAttackClient
{
    /// <summary>
    /// Receive an attack event (any flag submission for the game).
    /// </summary>
    public Task ReceivedAttack(AttackEvent evt);
}
