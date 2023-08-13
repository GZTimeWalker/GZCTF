using GZCTF.Extensions;
using GZCTF.Hubs;
using GZCTF.Hubs.Clients;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class SubmissionRepository : RepositoryBase, ISubmissionRepository
{
    private readonly IHubContext<MonitorHub, IMonitorClient> _hubContext;

    public SubmissionRepository(IHubContext<MonitorHub, IMonitorClient> hub,
        AppDbContext context) : base(context)
    {
        _hubContext = hub;
    }

    public async Task<Submission> AddSubmission(Submission submission, CancellationToken token = default)
    {
        await _context.AddAsync(submission, token);
        await _context.SaveChangesAsync(token);

        return submission;
    }

    public Task<Submission?> GetSubmission(int gameId, int challengeId, string userId, int submitId, CancellationToken token = default)
        => _context.Submissions.Where(s => s.Id == submitId && s.UserId == userId && s.GameId == gameId && s.ChallengeId == challengeId)
            .SingleOrDefaultAsync(token);

    public Task<Submission[]> GetUncheckedFlags(CancellationToken token = default)
        => _context.Submissions.Where(s => s.Status == AnswerResult.FlagSubmitted)
            .AsNoTracking().Include(e => e.Game).ToArrayAsync(token);

    private IQueryable<Submission> GetSubmissionsByType(AnswerResult? type = null)
    {
        var subs = type is not null
            ? _context.Submissions.Where(s => s.Status == type.Value)
            : _context.Submissions;

        return subs.OrderByDescending(s => s.SubmitTimeUTC);
    }

    public Task<Submission[]> GetSubmissions(Game game, AnswerResult? type = null, int count = 100, int skip = 0, CancellationToken token = default)
        => GetSubmissionsByType(type).Where(s => s.Game == game).TakeAllIfZero(count, skip).ToArrayAsync(token);

    public Task<Submission[]> GetSubmissions(Challenge challenge, AnswerResult? type = null, int count = 100, int skip = 0, CancellationToken token = default)
        => GetSubmissionsByType(type).Where(s => s.Challenge == challenge).TakeAllIfZero(count, skip).ToArrayAsync(token);

    public Task<Submission[]> GetSubmissions(Participation team, AnswerResult? type = null, int count = 100, int skip = 0, CancellationToken token = default)
        => GetSubmissionsByType(type).Where(s => s.TeamId == team.TeamId).TakeAllIfZero(count, skip).ToArrayAsync(token);

    public Task SendSubmission(Submission submission)
        => _hubContext.Clients.Group($"Game_{submission.GameId}").ReceivedSubmissions(submission);
}
