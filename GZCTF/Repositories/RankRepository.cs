using Microsoft.EntityFrameworkCore;
using CTFServer.Models;
using CTFServer.Models.Request;
using CTFServer.Repositories.Interface;
using CTFServer.Utils;
using NLog;

namespace CTFServer.Repositories;

public class RankRepository : RepositoryBase, IRankRepository
{
    private static readonly Logger logger = LogManager.GetLogger("RankRepository");

    public RankRepository(AppDbContext context) : base(context)
    {
    }

    public async Task CheckRankScore(CancellationToken token)
    {
        LogHelper.SystemLog(logger, "开始检查排名分数", TaskStatus.Pending);

        var users = context.Users.Include(u => u.Submissions).Include(u => u.Rank);

        foreach (UserInfo user in users)
        {
            int currentScore = 0;
            HashSet<int> challengesIds = new();

            foreach (var sub in user.Submissions.OrderByDescending(s => s.Score))
            {
                if (!challengesIds.Contains(sub.ChallengeId))
                {
                    currentScore += sub.Score;
                    challengesIds.Add(sub.ChallengeId);
                }
            }

            user.Rank!.Score = currentScore;

            LogHelper.SystemLog(logger, $"{user.UserName} 的分数为：{currentScore}", TaskStatus.Pending);
        }

        await context.SaveChangesAsync(token);
        LogHelper.SystemLog(logger, "检查排名分数完成");
    }

    public Task<List<RankMessageModel>> GetRank(CancellationToken token, int skip = 0, int count = 100)
        => (from rank in context.Ranks.OrderByDescending(r => r.Score)
                .Include(r => r.User).ThenInclude(u => u!.Submissions)
                .Where(r => r.User!.Privilege == Privilege.User)
                .Skip(skip).Take(count)
            select new RankMessageModel
            {
                Score = rank.Score,
                UpdateTime = rank.UpdateTimeUTC,
                UserName = rank.User!.UserName,
                Descr = rank.User.Description,
                UserId = rank.UserId,
                User = rank.User
            }).ToListAsync(token);

    public async Task UpdateRank(Rank rank, int score, CancellationToken token)
    {
        rank.UpdateTimeUTC = DateTimeOffset.UtcNow;
        rank.Score += score;
        await context.SaveChangesAsync(token);
        LogHelper.SystemLog(logger, $"增加分数：{rank.User!.UserName} => {score}");
    }
}
