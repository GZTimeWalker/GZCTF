using GZCTF.Models.Request.Admin;
using GZCTF.Models.Request.Game;

namespace GZCTF.Repositories.Interface;

public interface IParticipationRepository : IRepository
{
    /// <summary>
    /// Get the count of participants in a game
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<int> GetParticipationCount(int gameId, CancellationToken token = default);

    /// <summary>
    /// List all participations in a game
    /// </summary>
    /// <param name="game"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Participation[]> GetParticipations(Game game, CancellationToken token = default);

    /// <summary>
    /// Get participations by their IDs
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Participation[]> GetParticipationsByIds(IEnumerable<int> ids, CancellationToken token = default);

    /// <summary>
    /// Get teams for a user in a specific game, in which some participations may exist
    /// </summary>
    /// <param name="game"></param>
    /// <param name="user"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<JoinedTeam[]> GetJoinedTeams(Game game, UserInfo user, CancellationToken token = default);

    /// <summary>
    /// Get write-up information for all participations in a game
    /// </summary>
    /// <param name="game"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<WriteupInfoModel> GetWriteups(Game game, CancellationToken token = default);

    /// <summary>
    /// Make sure that the instances for a participation are created
    /// </summary>
    /// <param name="part"></param>
    /// <param name="game"></param>
    /// <param name="token"></param>
    /// <returns>If any instance was updated</returns>
    public Task<bool> EnsureInstances(Participation part, Game game, CancellationToken token = default);

    /// <summary>
    /// Check if the user has already participated in the game
    /// </summary>
    /// <param name="user"></param>
    /// <param name="game"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> CheckRepeatParticipation(UserInfo user, Game game, CancellationToken token = default);

    /// <summary>
    /// Remove user's participation in a specific game
    /// </summary>
    /// <param name="user"></param>
    /// <param name="game"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task RemoveUserParticipations(UserInfo user, Game game, CancellationToken token = default);

    /// <summary>
    /// Remove user's participation(s) in a specific team
    /// </summary>
    /// <param name="user"></param>
    /// <param name="team"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task RemoveUserParticipations(UserInfo user, Team team, CancellationToken token = default);

    /// <summary>
    /// Update the participation status and division
    /// </summary>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Participation?> GetParticipationById(int id, CancellationToken token = default);

    /// <summary>
    /// Get the participation object and its corresponding challenge list
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="gameId"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Participation?> GetParticipation(Guid userId, int gameId, CancellationToken token = default);

    /// <summary>
    /// Get the participation object for a specific team in a specific game
    /// </summary>
    /// <param name="team"></param>
    /// <param name="game"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Participation?> GetParticipation(Team team, Game game, CancellationToken token = default);

    /// <summary>
    /// Delete write-up information for a participation
    /// </summary>
    /// <param name="part"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task DeleteParticipationWriteUp(Participation part, CancellationToken token = default);

    /// <summary>
    /// Remove a participation
    /// </summary>
    /// <param name="part"></param>
    /// <param name="save"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task RemoveParticipation(Participation part, bool save = true, CancellationToken token = default);

    /// <summary>
    /// Update participation
    /// </summary>
    /// <param name="part"></param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task UpdateParticipation(Participation part, ParticipationEditModel model,
        CancellationToken token = default);

    /// <summary>
    /// Update participation status
    /// </summary>
    /// <param name="part"></param>
    /// <param name="status"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task UpdateParticipationStatus(Participation part, ParticipationStatus status,
        CancellationToken token = default);
}
