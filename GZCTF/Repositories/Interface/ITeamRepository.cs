using CTFServer.Models;
using CTFServer.Models.Request.Teams;

namespace CTFServer.Repositories.Interface;

public interface ITeamRepository : IRepository
{
    /// <summary>
    /// 创建队伍
    /// </summary>
    /// <param name="model">队伍信息</param>
    /// <param name="user">创建的用户</param>
    /// <param name="token"></param>
    /// <returns>队伍对象</returns>
    public Task<Team?> CreateTeam(TeamUpdateModel model, UserInfo user, CancellationToken token = default);

    /// <summary>
    /// 通过队伍Id获取队伍对象
    /// </summary>
    /// <param name="id">队伍Id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Team?> GetTeamById(int id, CancellationToken token = default);

    /// <summary>
    /// 获取当前用户激活的队伍及队员
    /// </summary>
    /// <param name="user">用户对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Team?> GetActiveTeamWithMembers(UserInfo user, CancellationToken token = default);

    /// <summary>
    /// 更新队伍信息
    /// </summary>
    /// <param name="team">队伍对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<int> UpdateAsync(Team team, CancellationToken token = default);

    /// <summary>
    /// 验证Token
    /// </summary>
    /// <param name="id">队伍Id</param>
    /// <param name="inviteToken">邀请Token</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> VeifyToken(int id, string inviteToken, CancellationToken token = default);

    /// <summary>
    /// 删除队伍
    /// </summary>
    /// <param name="team">删除队伍</param>
    /// <param name="token"></param>
    /// <returns>队伍对象</returns>
    public Task<int> DeleteTeam(Team team, CancellationToken token = default);

}
