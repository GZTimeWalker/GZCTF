using GZCTF.Models.Data.Cyctf;

namespace GZCTF.Repositories.Interface;

public interface IAwardRepository : IRepository
{
    /// <summary>
    /// 根据比赛 ID 获取所有奖项
    /// </summary>
    Task<List<Award>> GetAwardsByGameId(int gameId, CancellationToken token = default);

    /// <summary>
    /// 根据 ID 获取奖项
    /// </summary>
    Task<Award?> GetAwardById(int id, CancellationToken token = default);

    /// <summary>
    /// 创建奖项
    /// </summary>
    Task<Award> CreateAward(Award award, CancellationToken token = default);

    /// <summary>
    /// 更新奖项
    /// </summary>
    Task<Award?> UpdateAward(Award award, CancellationToken token = default);

    /// <summary>
    /// 删除奖项
    /// </summary>
    Task<bool> DeleteAward(int id, CancellationToken token = default);
}
