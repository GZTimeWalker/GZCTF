using CTFServer.Models;
using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Repositories;

public class GameEventRepository : RepositoryBase, IGameEventRepository
{
    public GameEventRepository(AppDbContext _context) : base(_context) { }

    public async Task<GameEvent> AddEvent(Game game, GameEvent gameEvent, CancellationToken token = default)
    {
        await context.Entry(game).Collection(e => e.GameEvents).LoadAsync(token);
        game.GameEvents.Add(gameEvent);
        await context.SaveChangesAsync(token);
        return gameEvent;
    }

    public Task<GameEvent[]> GetEvents(Game game, int count = 10, int skip = 0, CancellationToken token = default)
        => context.Entry(game).Collection(e => e.GameEvents).Query()
            .OrderByDescending(e => e.PublishTimeUTC).Skip(skip).Take(count).ToArrayAsync(token);
}
