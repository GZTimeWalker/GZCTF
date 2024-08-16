using GZCTF.Models.Request.Info;

namespace GZCTF.Repositories.Interface;

public interface ITeamRepository : IRepository
{
    /// <summary>
    /// 创建队伍
    /// </summary>
    /// <param name="model">队伍信息</param>
    /// <param name="user">创建的用户</param>
    /// <param name="token"></param>
    /// <returns>队伍对象</returns>
    public Task<Team> CreateTeam(TeamUpdateModel model, UserInfo user, CancellationToken token = default);

    /// <summary>
    /// 通过队伍Id获取队伍对象
    /// </summary>
    /// <param name="id">队伍Id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Team?> GetTeamById(int id, CancellationToken token = default);

    /// <summary>
    /// 通过用户获取队伍对象，含队员信息
    /// </summary>
    /// <param name="user">用户对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Team[]> GetUserTeams(UserInfo user, CancellationToken token = default);

    /// <summary>
    /// 检查是否为任意队伍的队长
    /// </summary>
    /// <param name="user">用户对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> CheckIsCaptain(UserInfo user, CancellationToken token = default);

    /// <summary>
    /// 搜索队伍
    /// </summary>
    /// <param name="hint">搜索字符串</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Team[]> SearchTeams(string hint, CancellationToken token = default);

    /// <summary>
    /// 获取队伍对象，含队员信息
    /// </summary>
    /// <param name="count">队伍数量</param>
    /// <param name="skip">跳过数量</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Team[]> GetTeams(int count = 100, int skip = 0, CancellationToken token = default);

    /// <summary>
    /// 是否有正在进行的比赛，比赛期间不允许进行人员变动
    /// </summary>
    /// <param name="team"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> AnyActiveGame(Team team, CancellationToken token = default);

    /// <summary>
    /// 移交队伍所有权
    /// </summary>
    /// <param name="team">队伍</param>
    /// <param name="user">新队长</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task Transfer(Team team, UserInfo user, CancellationToken token = default);

    /// <summary>
    /// 验证Token
    /// </summary>
    /// <param name="id">队伍Id</param>
    /// <param name="inviteToken">邀请Token</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> VerifyToken(int id, string inviteToken, CancellationToken token = default);

    /// <summary>
    /// 删除队伍
    /// </summary>
    /// <param name="team">删除队伍</param>
    /// <param name="token"></param>
    /// <returns>队伍对象</returns>
    public Task DeleteTeam(Team team, CancellationToken token = default);
}
