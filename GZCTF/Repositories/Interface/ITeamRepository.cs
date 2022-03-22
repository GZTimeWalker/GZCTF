using CTFServer.Models;

namespace CTFServer.Repositories.Interface;

public interface ITeamRepository
{
    /// <summary>
    /// 创建队伍
    /// </summary>
    /// <param name="name">队伍名称</param>
    /// <param name="user">创建的用户</param>
    /// <param name="token"></param>
    /// <returns>队伍对象</returns>
    public Task<Team?> CreateTeam(string name, UserInfo user, CancellationToken token = default);

    /// <summary>
    /// 通过队伍Id获取队伍对象
    /// </summary>
    /// <param name="id">队伍Id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Team?> GetTeamById(int id, CancellationToken token = default);

    /// <summary>
    /// 通过用户获取
    /// </summary>其所在的全部队伍
    /// <param name="user">用户对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<List<Team>> GetTeamsByUser(UserInfo user, CancellationToken token = default);

    /// <summary>
    /// 更新队伍信息
    /// </summary>
    /// <param name="team">队伍对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<int> UpdateAsync(Team team, CancellationToken token = default);
}
