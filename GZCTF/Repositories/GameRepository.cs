using CTFServer.Models;
using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Repositories;

public class GameRepository : RepositoryBase, IGameRepository
{
    public GameRepository(AppDbContext _context) : base(_context) { }

    public async Task<Game?> CreateGame(Game game, CancellationToken token = default)
    {
        await context.AddAsync(game, token);
        await context.SaveChangesAsync(token);
        return game;
    }

    public Task<Game?> GetGameById(int id, CancellationToken token = default)
        => context.Games.FirstOrDefaultAsync(x => x.Id == id, token);

    public Task<List<Game>> GetGames(int count = 10, int skip = 0, CancellationToken token = default)
        => context.Games.OrderByDescending(g => g.StartTimeUTC).Skip(skip).Take(count).ToListAsync(token);

    public Task<int> UpdateGame(Game game, CancellationToken token = default)
    {
        context.Update(game);
        return context.SaveChangesAsync(token);
    }
}
