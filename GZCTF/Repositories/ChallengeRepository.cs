using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Repositories;

public class ChallengeRepository : RepositoryBase, IChallengeRepository
{
    public ChallengeRepository(AppDbContext _context) : base(_context) { }

    public async Task<Challenge> CreateChallenge(Game game, Challenge challenge, CancellationToken token = default)
    {
        await context.AddAsync(challenge, token);
        game.Challenges.Add(challenge);
        await context.SaveChangesAsync(token);
        return challenge;
    }

    public Task EnableChallenge(Challenge challenge, CancellationToken token)
    {
        challenge.IsEnabled = true;
        context.Update(challenge);
        return context.SaveChangesAsync(token);
    }

    public Task<Challenge?> GetChallenge(int gameId, int id, CancellationToken token = default)
        => context.Challenges.Where(c => c.Id == id && c.GameId == gameId).FirstOrDefaultAsync(token);

    public Task<Challenge[]> GetChallenges(int gameId, int count = 100, int skip = 0, CancellationToken token = default)
        => context.Challenges.Where(c => c.GameId == gameId).OrderBy(c => c.Id).Skip(skip).Take(count).ToArrayAsync(token);

    public Task<bool> VerifyStaticAnswer(Challenge challenge, string flag, CancellationToken token = default)
        => context.Entry(challenge).Collection(e => e.Flags).Query().AnyAsync(f => f.Flag == flag, token);
}
