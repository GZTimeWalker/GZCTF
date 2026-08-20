using GZCTF.Models.Data.Cyctf;

namespace GZCTF.Repositories.Interface;

public interface IDivisionExtensionRepository : IRepository
{
    /// <summary>
    /// 根据组别 ID 获取扩展信息
    /// </summary>
    Task<DivisionExtension?> GetDivisionExtensionByDivisionId(int divisionId, CancellationToken token = default);

    /// <summary>
    /// 创建或更新组别扩展信息
    /// </summary>
    Task<DivisionExtension> CreateOrUpdateDivisionExtension(DivisionExtension extension, CancellationToken token = default);

    /// <summary>
    /// 删除组别扩展信息
    /// </summary>
    Task<bool> DeleteDivisionExtension(int divisionId, CancellationToken token = default);

    /// <summary>
    /// 检查组别是否有扩展信息
    /// </summary>
    Task<bool> HasDivisionExtension(int divisionId, CancellationToken token = default);

    /// <summary>
    /// 根据比赛 ID 获取所有组别扩展信息
    /// </summary>
    Task<List<DivisionExtension>> GetDivisionExtensionsByGameId(int gameId, CancellationToken token = default);
}
