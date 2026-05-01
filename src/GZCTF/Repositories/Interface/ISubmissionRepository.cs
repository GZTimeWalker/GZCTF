using GZCTF.Models.Request.Game;

namespace GZCTF.Repositories.Interface;

public interface ISubmissionRepository : IRepository
{
    /// <summary>
    /// Get submissions of a game, ordered by time descending
    /// </summary>
    /// <param name="game"></param>
    /// <param name="type">Type of submission</param>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="search">Search query to filter submissions</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Submission[]> GetSubmissions(Game game, AnswerResult? type = null, int count = 100, int skip = 0,
        string? search = null, CancellationToken token = default);

    /// <summary>
    /// Get submissions for a specific challenge, ordered by time descending
    /// </summary>
    /// <param name="challenge"></param>
    /// <param name="type">Type of submission</param>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Submission[]> GetSubmissions(GameChallenge challenge, AnswerResult? type = null, int count = 100,
        int skip = 0, CancellationToken token = default);

    /// <summary>
    /// Send submission to signalR hub
    /// </summary>
    /// <param name="submission"></param>
    public Task SendSubmission(Submission submission);

    /// <summary>
    /// Broadcast a precise <see cref="AttackEvent"/> (with known
    /// <see cref="SubmissionType"/>) on the public AttackHub. Used by
    /// FlagChecker right after VerifyAnswer so that blood colors reach
    /// the animation page accurately.
    /// </summary>
    public Task SendAttackEvent(Submission submission, SubmissionType type);

    /// <summary>
    /// Recent submissions for the public attack-feed seed. Includes both
    /// accepted and rejected answers so the page can render history on load.
    /// </summary>
    public Task<Submission[]> GetRecentSubmissionsForAttackFeed(int gameId, int limit,
        CancellationToken token = default);

    /// <summary>
    /// Get submissions for a specific team, ordered by time descending
    /// </summary>
    /// <param name="team"></param>
    /// <param name="type">Type of submission</param>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Submission[]> GetSubmissions(Participation team, AnswerResult? type = null, int count = 100,
        int skip = 0, CancellationToken token = default);

    /// <summary>
    /// Add a new submission
    /// </summary>
    /// <param name="submission"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Submission> AddSubmission(Submission submission, CancellationToken token = default);

    /// <summary>
    /// Get all unchecked flag submissions
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Submission[]> GetUncheckedFlags(CancellationToken token = default);

    /// <summary>
    /// Get a specific submission by gameId, challengeId, userId, and submitId
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="challengeId"></param>
    /// <param name="userId"></param>
    /// <param name="submitId"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Submission?> GetSubmission(int gameId, int challengeId, Guid userId, int submitId,
        CancellationToken token = default);

    /// <summary>
    /// Count submissions for a specific participation and challenge.
    /// </summary>
    public Task<int> CountSubmissions(int participationId, int challengeId, CancellationToken token = default);

    /// <summary>
    /// Count WrongAnswer submissions for a specific participation and challenge since a given time.
    /// Used for per-user per-challenge submission cooldown enforcement.
    /// </summary>
    public Task<int> CountRecentWrongSubmissions(int participationId, int challengeId, DateTimeOffset since,
        CancellationToken token = default);
}
