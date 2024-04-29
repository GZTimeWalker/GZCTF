using GZCTF.Models.Request.Admin;

namespace GZCTF.Repositories.Interface;

public interface IParticipationRepository : IRepository
{
    /// <summary>
    /// 获取比赛的报名队伍数量
    /// </summary>
    /// <param name="game">比赛对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<int> GetParticipationCount(Game game, CancellationToken token = default);

    /// <summary>
    /// 获取比赛参与列表
    /// </summary>
    /// <param name="game"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Participation[]> GetParticipations(Game game, CancellationToken token = default);

    /// <summary>
    /// 从 Id 数组获取比赛参与列表
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Participation[]> GetParticipationsByIds(IEnumerable<int> ids, CancellationToken token = default);

    /// <summary>
    /// 获取比赛 Writeup 列表
    /// </summary>
    /// <param name="game"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<WriteupInfoModel[]> GetWriteups(Game game, CancellationToken token = default);

    /// <summary>
    /// 确保此队伍 Instance 对象已创建
    /// </summary>
    /// <param name="part"></param>
    /// <param name="game"></param>
    /// <param name="token"></param>
    /// <returns>是否有实例更新</returns>
    public Task<bool> EnsureInstances(Participation part, Game game, CancellationToken token = default);

    /// <summary>
    /// 检查重复加入
    /// </summary>
    /// <param name="user">参赛用户</param>
    /// <param name="game">比赛</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<bool> CheckRepeatParticipation(UserInfo user, Game game, CancellationToken token = default);

    /// <summary>
    /// 移除用户的全部参与对象
    /// </summary>
    /// <param name="user">参赛用户</param>
    /// <param name="game">比赛</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task RemoveUserParticipations(UserInfo user, Game game, CancellationToken token = default);

    /// <summary>
    /// 移除用户的全部参与对象
    /// </summary>
    /// <param name="user">参赛用户</param>
    /// <param name="team">队伍对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task RemoveUserParticipations(UserInfo user, Team team, CancellationToken token = default);

    /// <summary>
    /// 通过 Id 更改参与状态
    /// </summary>
    /// <param name="id">参与对象Id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Participation?> GetParticipationById(int id, CancellationToken token = default);

    /// <summary>
    /// 获取比赛参与对象以及其对应题目列表
    /// </summary>
    /// <param name="user">用户对象</param>
    /// <param name="game">比赛对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Participation?> GetParticipation(UserInfo user, Game game, CancellationToken token = default);

    /// <summary>
    /// 获取比赛参与对象以及其对应题目列表
    /// </summary>
    /// <param name="team">比赛对象</param>
    /// <param name="game">比赛对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Participation?> GetParticipation(Team team, Game game, CancellationToken token = default);

    /// <summary>
    /// 删除参与对象的 WriteUps
    /// </summary>
    /// <param name="part">参与对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task DeleteParticipationWriteUp(Participation part, CancellationToken token = default);

    /// <summary>
    /// 删除参与对象
    /// </summary>
    /// <param name="part">参与对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task RemoveParticipation(Participation part, CancellationToken token = default);

    /// <summary>
    /// 更新比赛参与状态
    /// </summary>
    /// <param name="part">参与对象</param>
    /// <param name="status">参与状态</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task UpdateParticipationStatus(Participation part, ParticipationStatus status,
        CancellationToken token = default);
}
