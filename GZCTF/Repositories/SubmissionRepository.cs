using CTFServer.Models;
using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Repositories;

public class SubmissionRepository : RepositoryBase, ISubmissionRepository
{
    public SubmissionRepository(AppDbContext _context) : base(_context) { }

    public async Task<Submission> AddSubmission(Submission submission, CancellationToken token = default)
    {
        await context.AddAsync(submission, token);
        await context.SaveChangesAsync(token);

        return submission;
    }

    public Task<List<Submission>> GetSubmissions(int count = 100, int skip = 0, CancellationToken token = default)
        => context.Submissions.OrderByDescending(s => s.SubmitTimeUTC).Skip(skip).Take(count).ToListAsync(token);

    public Task<List<Submission>> GetSubmissions(Game game, int count = 100, int skip = 0, CancellationToken token = default)
        => context.Entry(game).Collection(g => g.Submissions)
        .Query().OrderByDescending(s => s.SubmitTimeUTC)
        .Skip(skip).Take(count).ToListAsync(token);


    public Task<List<Submission>> GetSubmissions(Challenge challenge, int count = 100, int skip = 0, CancellationToken token = default)
        => context.Entry(challenge).Collection(c => c.Submissions)
        .Query().OrderByDescending(s => s.SubmitTimeUTC)
        .Skip(skip).Take(count).ToListAsync(token);

    public Task<List<Submission>> GetSubmissions(Participation team, int count = 100, int skip = 0, CancellationToken token = default)
        => context.Entry(team).Collection(t => t.Submissions)
        .Query().OrderByDescending(s => s.SubmitTimeUTC)
        .Skip(skip).Take(count).ToListAsync(token);
}
