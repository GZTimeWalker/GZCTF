using GZCTF.Models.Request.Edit;

namespace GZCTF.Repositories.Interface;

public interface IDivisionRepository : IRepository
{
    /// <summary>
    /// Create a new division
    /// </summary>
    /// <param name="game"></param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<Division> CreateDivision(Game game, DivisionCreateModel model, CancellationToken token = default);

    /// <summary>
    /// Get all divisions for a specific game
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<Division[]> GetDivisions(int gameId, CancellationToken token = default);

    /// <summary>
    /// Get a specific division by game ID and division ID
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="divisionId"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<Division?> GetDivision(int gameId, int divisionId, CancellationToken token = default);

    /// <summary>
    /// Check if a division has a specific permission, optionally for a specific challenge
    /// </summary>
    /// <param name="divisionId"></param>
    /// <param name="permission"></param>
    /// <param name="challengeId"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    ValueTask<bool> CheckPermission(int? divisionId, GamePermission permission, int? challengeId,
        CancellationToken token = default);

    /// <summary>
    /// Update an existing division
    /// </summary>
    /// <param name="division"></param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task UpdateDivision(Division division, DivisionEditModel model, CancellationToken token = default);

    /// <summary>
    /// Remove a division
    /// </summary>
    /// <param name="division"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task RemoveDivision(Division division, CancellationToken token = default);
}
