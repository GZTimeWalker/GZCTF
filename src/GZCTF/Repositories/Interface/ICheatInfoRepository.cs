namespace GZCTF.Repositories.Interface;

public interface ICheatInfoRepository : IRepository
{
    /// <summary>
    /// Create a new cheat info record
    /// </summary>
    /// <param name="submission">The submission that triggered the cheat info</param>
    /// <param name="source">The instance where the flag was pointed</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<CheatInfo> CreateCheatInfo(Submission submission, GameInstance source,
        CancellationToken token = default);

    /// <summary>
    /// Get all cheat info records for a specific game by its ID
    /// </summary>
    /// <param name="gameId">The game ID</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<CheatInfo[]> GetCheatInfoByGameId(int gameId, CancellationToken token = default);
}
