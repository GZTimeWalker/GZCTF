using CTFServer.Models;
using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Repositories;

public class EventRepository : RepositoryBase, IEventRepository
{
    public EventRepository(AppDbContext _context) : base(_context) { }

    public async Task<Event> AddEvent(Game game, Event @event, CancellationToken token = default)
    {
        await context.Entry(game).Collection(e => e.Events).LoadAsync(token);
        game.Events.Add(@event);
        await context.SaveChangesAsync(token);
        return @event;
    }

    public Task<Event[]> GetEvents(Game game, int count = 10, int skip = 0, CancellationToken token = default)
        => context.Entry(game).Collection(e => e.Events).Query()
            .OrderByDescending(e => e.PublishTimeUTC).Skip(skip).Take(count).ToArrayAsync(token);

}
