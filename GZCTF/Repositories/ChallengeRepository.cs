using Microsoft.EntityFrameworkCore;
using CTFServer.Models;
using CTFServer.Models.Request;
using CTFServer.Repositories.Interface;
using CTFServer.Utils;
using NLog;

namespace CTFServer.Repositories;

public class ChallengeRepository : RepositoryBase, IChallengeRepository
{
    private static readonly Logger logger = LogManager.GetLogger("ChallengesRepository");

    public ChallengeRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Challenge> AddChallenge(ChallengeBase newChallenge, CancellationToken token)
    {
        Challenge challenge = new(newChallenge);

        await context.AddAsync(challenge, token);
        await context.SaveChangesAsync(token);

        LogHelper.SystemLog(logger, $"添加题目#{challenge.Id} {challenge.Title} 成功");

        return challenge;
    }

    public async Task<(bool result, string title)> DeleteChallenges(int id, CancellationToken token)
    {
        Challenge? challenge = await context.Challenges.FirstOrDefaultAsync(x => x.Id == id, token);

        if (challenge is null)
        {
            LogHelper.SystemLog(logger, $"未找到试图删除的题目", TaskStatus.Fail);
            return (false, string.Empty);
        }

        string title = challenge.Title!;

        context.Remove(challenge);
        await context.SaveChangesAsync(token);

        return (true, title);
    }

    public Task<List<ChallengesItem>> GetAccessibleChallengess(int accessLevel, CancellationToken token)
        => (from p in context.Challenges
                select new ChallengesItem()
                {
                    Id = p.Id,
                    Title = p.Title,
                    AcceptedCount = p.AcceptedCount,
                    SubmissionCount = p.SubmissionCount,
                    Score = p.CurrentScore
                }).ToListAsync(token);

    public async Task<UserChallengesModel?> GetUserChallenges(int id, int accessLevel, CancellationToken token)
    {
        Challenge? challenge = await context.Challenges.FirstOrDefaultAsync(x => x.Id == id, token);

        if (challenge is null)
        {
            LogHelper.SystemLog(logger, $"未找到满足要求的题目", TaskStatus.Fail);
            return null;
        }

        return new UserChallengesModel
        {
            Title = challenge.Title,
            Content = challenge.Content,
            AcceptedCount = challenge.AcceptedUserCount,
            SubmissionCount = challenge.SubmissionCount
        };
    }

    public async Task<Challenge?> UpdateChallenges(int id, ChallengeBase newChallenge, CancellationToken token)
    {
        Challenge? challenge = await context.Challenges.FirstOrDefaultAsync(x => x.Id == id, token);

        if (challenge is null)
        {
            LogHelper.SystemLog(logger, $"未找到满足要求的题目", TaskStatus.Fail);
            return null;
        }

        challenge.Update(newChallenge);
        await context.SaveChangesAsync(token);

        LogHelper.SystemLog(logger, $"成功更新题目#{challenge.Id}");

        return challenge;
    }

    public async Task<VerifyResult> VerifyAnswer(int id, string? answer, UserInfo user, bool hasSolved, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            LogHelper.SystemLog(logger, "答案验证遇到空输入", TaskStatus.Fail);
            return new VerifyResult();
        }

        Challenge? challenge = await context.Challenges.FirstOrDefaultAsync(x => x.Id == id, token);

        if (challenge is null)
        {
            LogHelper.SystemLog(logger, $"题目未找到#{id}", TaskStatus.Fail);
            return new VerifyResult(AnswerResult.Unauthorized);
        }

        /*if(challenge.AccessLevel > user.AccessLevel)
        {
            LogHelper.SystemLog(logger, $"未授权的题目访问#{id}", TaskStatus.Denied);
            return new VerifyResult(AnswerResult.Unauthorized);
        }*/

        /*bool check = string.Equals(challenge.Answer, answer);

        //var result = check ? new VerifyResult(AnswerResult.Accepted, challenge.CurrentScore, challenge.UpgradeAccessLevel)
        //        : new VerifyResult(AnswerResult.WrongAnswer);

        if (user.Privilege != Privilege.User)
            return AnswerResult.WrongAnswer;

        ++challenge.SubmissionCount;

        if (check)
        {
            ++challenge.AcceptedCount;

            if (!hasSolved)
                ++challenge.AcceptedUserCount;
        }

        await context.SaveChangesAsync(token);

        return AnswerResult.Accepted;*/
        throw new NotImplementedException();
    }
}
