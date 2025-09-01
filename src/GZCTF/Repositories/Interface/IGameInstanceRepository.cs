using GZCTF.Models.Internal;

namespace GZCTF.Repositories.Interface;

public interface IGameInstanceRepository : IRepository
{
    /// <summary>
    /// Get or create a game instance for a team and challenge
    /// </summary>
    /// <param name="team">Team</param>
    /// <param name="challengeId">Challenge id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<GameInstance?> GetInstance(Participation team, int challengeId, CancellationToken token = default);

    /// <summary>
    /// Verify the answer of a submission
    /// </summary>
    /// <param name="submission">Submission</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<VerifyResult> VerifyAnswer(Submission submission, CancellationToken token = default);

    /// <summary>
    /// Fetch and add submission count for a team and challenge
    /// </summary>
    /// <param name="team"></param>
    /// <param name="challenge"></param>
    /// <param name="token"></param>
    /// <returns>True if the team can submit, false if the limit is reached</returns>
    public Task<bool> FetchAddSubmissionCount(Participation team, GameChallenge challenge,
        CancellationToken token = default);

    /// <summary>
    /// Check if the submission is a cheat
    /// </summary>
    /// <param name="submission">Submission</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<CheatCheckInfo> CheckCheat(Submission submission, CancellationToken token = default);

    /// <summary>
    /// Create a container for a game instance
    /// </summary>
    /// <param name="team">Team info</param>
    /// <param name="game">Game</param>
    /// <param name="user">User</param>
    /// <param name="gameInstance">Instance</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<TaskResult<Container>> CreateContainer(GameInstance gameInstance, Team team, UserInfo user,
        Game game, CancellationToken token = default);

    /// <summary>
    /// Destroy all instances of a challenge
    /// </summary>
    /// <param name="challenge">Challenge</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task DestroyAllInstances(GameChallenge challenge, CancellationToken token = default);
}
