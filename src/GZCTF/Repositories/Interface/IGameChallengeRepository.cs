using GZCTF.Models.Request.Edit;

namespace GZCTF.Repositories.Interface;

public interface IGameChallengeRepository : IRepository
{
    /// <summary>
    /// Create a new challenge for a game
    /// </summary>
    /// <param name="game"></param>
    /// <param name="challenge"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameChallenge> CreateChallenge(Game game, GameChallenge challenge, CancellationToken token = default);

    /// <summary>
    /// Remove a challenge
    /// </summary>
    /// <param name="challenge"></param>
    /// <param name="save"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task RemoveChallenge(GameChallenge challenge, bool save = true, CancellationToken token = default);

    /// <summary>
    /// Get all challenges for a game
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameChallenge[]> GetChallenges(int gameId, CancellationToken token = default);

    /// <summary>
    /// Get a specific challenge by game ID and challenge ID, without loading Flags
    /// </summary>
    /// <param name="gameId">比赛Id</param>
    /// <param name="id">题目Id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameChallenge?> GetChallenge(int gameId, int id, CancellationToken token = default);

    /// <summary>
    /// Load Flags for a challenge, the challenge should be tracked by the DbContext
    /// </summary>
    /// <param name="challenge"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task LoadFlags(GameChallenge challenge, CancellationToken token = default);

    /// <summary>
    /// Get challenges that require traffic capturing for a game
    /// </summary>
    /// <param name="gameId">比赛Id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameChallenge[]> GetChallengesWithTrafficCapturing(int gameId, CancellationToken token = default);

    /// <summary>
    /// Add Flags to a challenge
    /// </summary>
    /// <param name="challenge"></param>
    /// <param name="model">Flag information</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task AddFlags(GameChallenge challenge, FlagCreateModel[] model, CancellationToken token = default);

    /// <summary>
    /// Make sure all teams in the game have instances for the challenge
    /// </summary>
    /// <param name="challenge"></param>
    /// <param name="game"></param>
    /// <param name="token"></param>
    /// <returns>If any instance was updated</returns>
    public Task<bool> EnsureInstances(GameChallenge challenge, Game game, CancellationToken token = default);

    /// <summary>
    /// Update the attachment of a challenge
    /// </summary>
    /// <param name="challenge"></param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task UpdateAttachment(GameChallenge challenge, AttachmentCreateModel model,
        CancellationToken token = default);

    /// <summary>
    /// Remove a flag from a challenge, ensuring that submissions are loaded
    /// </summary>
    /// <param name="challenge"></param>
    /// <param name="flagId">flag ID</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<TaskStatus> RemoveFlag(GameChallenge challenge, int flagId, CancellationToken token = default);
}
