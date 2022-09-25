using CTFServer.Extensions;
using CTFServer.Hubs;
using CTFServer.Hubs.Clients;
using CTFServer.Repositories.Interface;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.Formula.Functions;

namespace CTFServer.Repositories;

public class SubmissionRepository : RepositoryBase, ISubmissionRepository
{
    private readonly IHubContext<MonitorHub, IMonitorClient> hubContext;

    public SubmissionRepository(IHubContext<MonitorHub, IMonitorClient> hub,
        AppDbContext _context) : base(_context)
    {
        hubContext = hub;
    }

    public async Task<Submission> AddSubmission(Submission submission, CancellationToken token = default)
    {
        await context.AddAsync(submission, token);
        await context.SaveChangesAsync(token);

        return submission;
    }

    public Task<Submission?> GetSubmission(int gameId, int challengeId, string userId, int submitId, CancellationToken token = default)
        => context.Submissions.Where(s => s.Id == submitId && s.UserId == userId && s.GameId == gameId && s.ChallengeId == challengeId)
            .SingleOrDefaultAsync(token);

    public Task<Submission[]> GetUncheckedFlags(CancellationToken token = default)
        => context.Submissions.Where(s => s.Status == AnswerResult.FlagSubmitted)
            .AsNoTracking().Include(e => e.Game).ToArrayAsync(token);

    private IQueryable<Submission> GetSubmissionsByType(AnswerResult? type = null)
    {
        var subs = type is not null
            ? context.Submissions.Where(s => s.Status == type.Value)
            : context.Submissions;

        return subs.OrderByDescending(s => s.SubmitTimeUTC);
    }

    public Task<Submission[]> GetSubmissions(Game game, AnswerResult? type = null, int count = 100, int skip = 0, CancellationToken token = default)
        => GetSubmissionsByType(type).Where(s => s.Game == game).TakeAllIfZero(count, skip).ToArrayAsync(token);

    public Task<Submission[]> GetSubmissions(Challenge challenge, AnswerResult? type = null, int count = 100, int skip = 0, CancellationToken token = default)
        => GetSubmissionsByType(type).Where(s => s.Challenge == challenge).TakeAllIfZero(count, skip).ToArrayAsync(token);

    public Task<Submission[]> GetSubmissions(Participation team, AnswerResult? type = null, int count = 100, int skip = 0, CancellationToken token = default)
        => GetSubmissionsByType(type).Where(s => s.TeamId == team.TeamId).TakeAllIfZero(count, skip).ToArrayAsync(token);

    public Task SendSubmission(Submission submission)
        => hubContext.Clients.Group($"Game_{submission.GameId}").ReceivedSubmissions(submission);
}