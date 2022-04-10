using CTFServer.Models;

namespace CTFServer.Repositories.Interface;

public interface IParticipationRepository : IRepository
{
    /// <summary>
    /// 创建比赛对象
    /// </summary>
    /// <param name="team">队伍</param>
    /// <param name="game">比赛</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Participation> CreateParticipation(Team team, Game game, CancellationToken token = default);
    /// <summary>
    /// 获取比赛参与对象以及其对应题目列表
    /// </summary>
    /// <param name="team">队伍对象</param>
    /// <param name="game">比赛对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<Participation?> GetParticipation(Team team, Game game, CancellationToken token = default);
}
