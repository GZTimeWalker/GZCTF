using GZCTF.Models.Request.Info;

namespace GZCTF.Repositories.Interface;

public interface ITeamRepository : IRepository
{
    /// <summary>
    /// Create a new team
    /// </summary>
    /// <param name="model"></param>
    /// <param name="user"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Team> CreateTeam(TeamUpdateModel model, UserInfo user, CancellationToken token = default);

    /// <summary>
    /// Get team by ID
    /// </summary>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Team?> GetTeamById(int id, CancellationToken token = default);

    /// <summary>
    /// Get teams by a user, including member information
    /// </summary>
    /// <param name="user">用户对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Team[]> GetUserTeams(UserInfo user, CancellationToken token = default);

    /// <summary>
    /// Check if the user is a team captain
    /// </summary>
    /// <param name="user"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> CheckIsCaptain(UserInfo user, CancellationToken token = default);

    /// <summary>
    /// Search teams by a hint string
    /// </summary>
    /// <param name="hint"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Team[]> SearchTeams(string hint, CancellationToken token = default);

    /// <summary>
    /// Get a list of teams with pagination, with member information
    /// </summary>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Team[]> GetTeams(int count = 100, int skip = 0, CancellationToken token = default);

    /// <summary>
    /// Check if the team is currently participating in any active games
    /// </summary>
    /// <param name="team"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> AnyActiveGame(Team team, CancellationToken token = default);

    /// <summary>
    /// Transfer team leadership
    /// </summary>
    /// <param name="team"></param>
    /// <param name="user"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task Transfer(Team team, UserInfo user, CancellationToken token = default);

    /// <summary>
    /// Verify the invitation token for joining a team
    /// </summary>
    /// <param name="id"></param>
    /// <param name="inviteToken"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> VerifyToken(int id, string inviteToken, CancellationToken token = default);

    /// <summary>
    /// Remove a team
    /// </summary>
    /// <param name="team"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task DeleteTeam(Team team, CancellationToken token = default);
}
