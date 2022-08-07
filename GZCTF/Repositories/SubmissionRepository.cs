using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Repositories;

public class SubmissionRepository : RepositoryBase, ISubmissionRepository
{
    public SubmissionRepository(AppDbContext _context) : base(_context)
    {
    }

    public async Task<Submission> AddSubmission(Submission submission, CancellationToken token = default)
    {
        await context.AddAsync(submission, token);
        await context.SaveChangesAsync(token);

        return submission;
    }

    public Task UpdateSubmission(Submission submission, CancellationToken token = default)
    {
        context.Update(submission);
        return SaveAsync(token);
    }

    public Task<Submission[]> GetSubmissions(int count = 100, int skip = 0, CancellationToken token = default)
        => context.Submissions.OrderByDescending(s => s.SubmitTimeUTC).Skip(skip).Take(count).ToArrayAsync(token);

    public Task<Submission[]> GetSubmissions(Game game, int count = 100, int skip = 0, CancellationToken token = default)
        => context.Entry(game).Collection(g => g.Submissions)
        .Query().OrderByDescending(s => s.SubmitTimeUTC)
        .Skip(skip).Take(count).ToArrayAsync(token);

    public Task<Submission[]> GetSubmissions(Challenge challenge, int count = 100, int skip = 0, CancellationToken token = default)
        => context.Entry(challenge).Collection(c => c.Submissions)
        .Query().OrderByDescending(s => s.SubmitTimeUTC)
        .Skip(skip).Take(count).ToArrayAsync(token);

    public Task<Submission[]> GetSubmissions(Participation team, int count = 100, int skip = 0, CancellationToken token = default)
        => context.Entry(team).Collection(t => t.Submissions)
        .Query().OrderByDescending(s => s.SubmitTimeUTC)
        .Skip(skip).Take(count).ToArrayAsync(token);

    public Task<Submission?> GetSubmission(int gameId, int challengeId, string userId, int submitId, CancellationToken token = default)
        => context.Submissions.Where(s => s.Id == submitId && s.UserId == userId && s.GameId == gameId && s.ChallengeId == challengeId)
            .FirstOrDefaultAsync(token);

    public Task<Submission[]> GetUncheckedFlags(CancellationToken token = default)
        => context.Submissions.Where(s => s.Status == AnswerResult.FlagSubmitted)
            .AsNoTracking()
            .Include(s => s.User).Include(s => s.Team).Include(s => s.Challenge)
            .ToArrayAsync(token);
}