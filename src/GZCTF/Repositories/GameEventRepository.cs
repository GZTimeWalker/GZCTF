using GZCTF.Hubs;
using GZCTF.Hubs.Clients;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class GameEventRepository : RepositoryBase, IGameEventRepository
{
    private readonly IHubContext<MonitorHub, IMonitorClient> _hubContext;

    public GameEventRepository(
        IHubContext<MonitorHub, IMonitorClient> hub,
        AppDbContext context) : base(context)
    {
        _hubContext = hub;
    }

    public async Task<GameEvent> AddEvent(GameEvent gameEvent, CancellationToken token = default)
    {
        await _context.AddAsync(gameEvent, token);
        await SaveAsync(token);

        gameEvent = await _context.GameEvents.SingleAsync(s => s.Id == gameEvent.Id, token);

        await _hubContext.Clients.Group($"Game_{gameEvent.GameId}")
                .ReceivedGameEvent(gameEvent);

        return gameEvent;
    }

    public Task<GameEvent[]> GetEvents(int gameId, bool hideContainer = false, int count = 50, int skip = 0, CancellationToken token = default)
    {
        var data = _context.GameEvents.Where(e => e.GameId == gameId);

        if (hideContainer)
            data = data.Where(e => e.Type != EventType.ContainerStart && e.Type != EventType.ContainerDestroy);

        return data.OrderByDescending(e => e.PublishTimeUTC).Skip(skip).Take(count).ToArrayAsync(token);
    }
}
