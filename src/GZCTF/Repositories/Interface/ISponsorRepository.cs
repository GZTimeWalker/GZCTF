using GZCTF.Models.Data.Cyctf;

namespace GZCTF.Repositories.Interface;

public interface ISponsorRepository : IRepository
{
    /// <summary>
    /// 根据比赛 ID 获取所有赞助商
    /// </summary>
    Task<List<Sponsor>> GetSponsorsByGameId(int gameId, CancellationToken token = default);

    /// <summary>
    /// 根据 ID 获取赞助商
    /// </summary>
    Task<Sponsor?> GetSponsorById(int id, CancellationToken token = default);

    /// <summary>
    /// 创建赞助商
    /// </summary>
    Task<Sponsor> CreateSponsor(Sponsor sponsor, CancellationToken token = default);

    /// <summary>
    /// 更新赞助商
    /// </summary>
    Task<Sponsor?> UpdateSponsor(Sponsor sponsor, CancellationToken token = default);

    /// <summary>
    /// 删除赞助商
    /// </summary>
    Task<bool> DeleteSponsor(int id, CancellationToken token = default);
}
