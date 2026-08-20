using GZCTF.Models.Data.Cyctf;

namespace GZCTF.Repositories.Interface;

public interface IGameExtensionRepository : IRepository
{
    /// <summary>
    /// 根据比赛 ID 获取扩展信息
    /// </summary>
    Task<GameExtension?> GetGameExtensionByGameId(int gameId, CancellationToken token = default);

    /// <summary>
    /// 创建或更新比赛扩展信息
    /// </summary>
    Task<GameExtension> CreateOrUpdateGameExtension(GameExtension extension, CancellationToken token = default);

    /// <summary>
    /// 删除比赛扩展信息
    /// </summary>
    Task<bool> DeleteGameExtension(int gameId, CancellationToken token = default);

    /// <summary>
    /// 检查比赛是否有扩展信息
    /// </summary>
    Task<bool> HasGameExtension(int gameId, CancellationToken token = default);

    /// <summary>
    /// 更新当前报名队伍数量
    /// </summary>
    Task UpdateCurrentTeams(int gameId, int count, CancellationToken token = default);
}
