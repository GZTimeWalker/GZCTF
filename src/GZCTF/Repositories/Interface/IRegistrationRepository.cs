using GZCTF.Models.Data.Cyctf;

namespace GZCTF.Repositories.Interface;

public interface IRegistrationRepository : IRepository
{
    /// <summary>
    /// 根据比赛 ID 获取所有报名记录
    /// </summary>
    Task<List<Registration>> GetRegistrationsByGameId(int gameId, CancellationToken token = default);

    /// <summary>
    /// 根据队伍 ID 和比赛 ID 获取报名记录
    /// </summary>
    Task<Registration?> GetRegistrationByTeamAndGame(int teamId, int gameId, CancellationToken token = default);

    /// <summary>
    /// 根据 ID 获取报名记录
    /// </summary>
    Task<Registration?> GetRegistrationById(int id, CancellationToken token = default);

    /// <summary>
    /// 创建报名记录
    /// </summary>
    Task<Registration> CreateRegistration(Registration registration, CancellationToken token = default);

    /// <summary>
    /// 更新报名状态
    /// </summary>
    Task<Registration?> UpdateRegistrationStatus(int id, string status, string? reviewNote, Guid? reviewedBy, CancellationToken token = default);

    /// <summary>
    /// 检查队伍是否已报名
    /// </summary>
    Task<bool> HasRegistration(int teamId, int gameId, CancellationToken token = default);

    /// <summary>
    /// 获取比赛的报名统计
    /// </summary>
    Task<Dictionary<string, int>> GetRegistrationStats(int gameId, CancellationToken token = default);
}
