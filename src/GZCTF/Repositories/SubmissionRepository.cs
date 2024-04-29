using GZCTF.Extensions;
using GZCTF.Hubs;
using GZCTF.Hubs.Clients;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class SubmissionRepository(
    IHubContext<MonitorHub, IMonitorClient> hub,
    AppDbContext context) : RepositoryBase(context), ISubmissionRepository
{
    public async Task<Submission> AddSubmission(Submission submission, CancellationToken token = default)
    {
        await Context.AddAsync(submission, token);
        await Context.SaveChangesAsync(token);

        return submission;
    }

    public Task<Submission?> GetSubmission(int gameId, int challengeId, Guid userId, int submitId,
        CancellationToken token = default)
        => Context.Submissions.Where(s =>
                s.Id == submitId && s.UserId == userId && s.GameId == gameId && s.ChallengeId == challengeId)
            .SingleOrDefaultAsync(token);

    public Task<Submission[]> GetUncheckedFlags(CancellationToken token = default) =>
        Context.Submissions.Where(s => s.Status == AnswerResult.FlagSubmitted)
            .AsNoTracking().Include(e => e.Game).ToArrayAsync(token);

    public Task<Submission[]> GetSubmissions(Game game, AnswerResult? type = null, int count = 100, int skip = 0,
        CancellationToken token = default) =>
        GetSubmissionsByType(type).Where(s => s.Game == game).TakeAllIfZero(count, skip).ToArrayAsync(token);

    public Task<Submission[]> GetSubmissions(GameChallenge challenge, AnswerResult? type = null, int count = 100,
        int skip = 0, CancellationToken token = default) =>
        GetSubmissionsByType(type).Where(s => s.GameChallenge == challenge).TakeAllIfZero(count, skip)
            .ToArrayAsync(token);

    public Task<Submission[]> GetSubmissions(Participation team, AnswerResult? type = null, int count = 100,
        int skip = 0, CancellationToken token = default) =>
        GetSubmissionsByType(type).Where(s => s.TeamId == team.TeamId).TakeAllIfZero(count, skip)
            .ToArrayAsync(token);

    public Task SendSubmission(Submission submission)
        => hub.Clients.Group($"Game_{submission.GameId}").ReceivedSubmissions(submission);

    IQueryable<Submission> GetSubmissionsByType(AnswerResult? type = null)
    {
        IQueryable<Submission> subs = type is not null
            ? Context.Submissions.Where(s => s.Status == type.Value)
            : Context.Submissions;

        return subs.OrderByDescending(s => s.SubmitTimeUtc);
    }
}
