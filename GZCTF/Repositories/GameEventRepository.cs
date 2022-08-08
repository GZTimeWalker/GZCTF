using CTFServer.Hubs;
using CTFServer.Hubs.Clients;
using CTFServer.Repositories.Interface;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Repositories;

public class GameEventRepository : RepositoryBase, IGameEventRepository
{
    private IHubContext<MonitorHub, IMonitorClient> hubContext;

    public GameEventRepository(
        IHubContext<MonitorHub, IMonitorClient> hub,
        AppDbContext _context) : base(_context)
    {
        hubContext = hub;
    }

    public async Task<GameEvent> AddEvent(GameEvent gameEvent, CancellationToken token = default)
    {
        await context.AddAsync(gameEvent);
        await SaveAsync(token);

        gameEvent = await context.GameEvents.SingleAsync(s => s.Id == gameEvent.Id, token);

        await hubContext.Clients.Group($"Game_{gameEvent.GameId}")
                .ReceivedGameEvent(gameEvent);

        return gameEvent;
    }

    public Task<GameEvent[]> GetEvents(int gameId, int count = 50, int skip = 0, CancellationToken token = default)
        => context.GameEvents.Where(e => e.GameId == gameId)
            .OrderByDescending(e => e.PublishTimeUTC)
            .Skip(skip).Take(count)
            .ToArrayAsync(token);
}