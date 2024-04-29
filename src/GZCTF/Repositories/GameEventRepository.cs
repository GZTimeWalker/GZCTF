using GZCTF.Hubs;
using GZCTF.Hubs.Clients;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class GameEventRepository(
    IHubContext<MonitorHub, IMonitorClient> hub,
    AppDbContext context) : RepositoryBase(context), IGameEventRepository
{
    public async Task<GameEvent> AddEvent(GameEvent gameEvent, CancellationToken token = default)
    {
        await Context.AddAsync(gameEvent, token);
        await SaveAsync(token);

        gameEvent = await Context.GameEvents.SingleAsync(s => s.Id == gameEvent.Id, token);

        await hub.Clients.Group($"Game_{gameEvent.GameId}")
            .ReceivedGameEvent(gameEvent);

        return gameEvent;
    }

    public Task<GameEvent[]> GetEvents(int gameId, bool hideContainer = false, int count = 50, int skip = 0,
        CancellationToken token = default)
    {
        IQueryable<GameEvent> data = Context.GameEvents.Where(e => e.GameId == gameId);

        if (hideContainer)
            data = data.Where(e => e.Type != EventType.ContainerStart && e.Type != EventType.ContainerDestroy);

        return data.OrderByDescending(e => e.PublishTimeUtc).Skip(skip).Take(count).ToArrayAsync(token);
    }
}
