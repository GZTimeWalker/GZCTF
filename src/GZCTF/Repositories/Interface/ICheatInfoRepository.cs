namespace GZCTF.Repositories.Interface;

public interface ICheatInfoRepository : IRepository
{
    /// <summary>
    /// 创建作弊信息对象
    /// </summary>
    /// <param name="submission">提交对象</param>
    /// <param name="source">flag 所属实例</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<CheatInfo> CreateCheatInfo(Submission submission, GameInstance source,
        CancellationToken token = default);

    /// <summary>
    /// 获取作弊信息对象
    /// </summary>
    /// <param name="gameId">比赛Id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<CheatInfo[]> GetCheatInfoByGameId(int gameId, CancellationToken token = default);
}
