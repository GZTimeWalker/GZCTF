using Microsoft.EntityFrameworkCore;
using CTFServer.Models;
using CTFServer.Models.Request;
using CTFServer.Repositories.Interface;
using CTFServer.Utils;
using NLog;

namespace CTFServer.Repositories;

public class SubmissionRepository : RepositoryBase, ISubmissionRepository
{
    private static readonly Logger logger = LogManager.GetLogger("SubmissionRepository");
    private readonly IConfiguration configuration;

    public SubmissionRepository(AppDbContext context, IConfiguration config) : base(context)
    {
        configuration = config;
    }

    public async Task AddSubmission(Submission sub, CancellationToken token)
    {
        await context.AddAsync(sub, token);
        await context.SaveChangesAsync(token);
    }

    public async Task<HashSet<int>> GetSolvedChallengess(string userId, CancellationToken token)
        // => (await (from sub in context.Submissions.Where(s => s.Solved && s.UserId == userId)
        //            select sub.ChallengeId).ToListAsync(token)).ToHashSet();
        => throw new NotImplementedException();

    public Task<List<Submission>> GetSubmissions(CancellationToken token, int skip = 0, int count = 50, int challengesId = 0, string userId = "All")
    {
        IQueryable<Submission> result = context.Submissions;

        //if (challengesId > 0)
        //    result = result.Where(s => s.ChallengeId == challengesId);
        throw new NotImplementedException();

        if (userId != "All")
            result = result.Where(s => s.UserId == userId);

        return result.OrderByDescending(s => s.SubmitTimeUTC).Skip(skip).Take(count).ToListAsync(token);
    }

    public List<TimeLineModel> GetTimeLine(UserInfo user, CancellationToken token)
    {
        if (user is null || user.Submissions is null)
            return new();

        int currentScore = 0;
        HashSet<int> challengesIds = new();
        List<TimeLineModel> result = new();

        var success = DateTimeOffset.TryParse(configuration["StartTime"], out DateTimeOffset start);

        result.Add(new TimeLineModel
        {
            Time = (success && start > user.RegisterTimeUTC) ? start : user.RegisterTimeUTC,
            TotalScore = 0,
            challengesId = -1,
        });

        throw new NotImplementedException();

        /*foreach (var sub in user.Submissions.OrderBy(s => s.SubmitTimeUTC))
        {
            if (!challengesIds.Contains(sub.ChallengeId) && sub.Score != 0)
            {
                currentScore += sub.Score;
                result.Add(new TimeLineModel()
                {
                    challengesId = sub.ChallengeId,
                    Time = sub.SubmitTimeUTC,
                    TotalScore = currentScore
                });
                challengesIds.Add(sub.ChallengeId);
            }
        }*/

        return result;
    }

    public Task<bool> HasSubmitted(int challengesId, string userId, CancellationToken token)
       // => context.Submissions.AnyAsync(s => s.ChallengeId == challengesId && s.UserId == userId && s.Solved, token);
        => throw new NotImplementedException();
}
