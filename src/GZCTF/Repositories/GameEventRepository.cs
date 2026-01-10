using GZCTF.Hubs;
using GZCTF.Hubs.Clients;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

using GZCTF.Services.Webhook;

namespace GZCTF.Repositories;

public class GameEventRepository(
    IHubContext<MonitorHub, IMonitorClient> hub,
    ISendWebhookService webhookService,
    AppDbContext context) : RepositoryBase(context), IGameEventRepository
{
    public async Task<GameEvent> AddEvent(GameEvent gameEvent, CancellationToken token = default)
    {
        await Context.AddAsync(gameEvent, token);
        await SaveAsync(token);

        gameEvent = await Context.GameEvents
            .Include(e => e.User)
            .Include(e => e.Team)
            .Include(e => e.Game)
            .SingleAsync(s => s.Id == gameEvent.Id, token);

        await hub.Clients.Group($"Game_{gameEvent.GameId}").ReceivedGameEvent(gameEvent);

        if (gameEvent.Game?.DiscordWebhook is { Length: > 0 } webhookUrl)
        {
            _ = webhookService.SendGameEventAsync(gameEvent, webhookUrl);
        }

        return gameEvent;
    }

    public Task<GameEvent[]> GetEvents(int gameId, bool hideContainer = false, int count = 50, int skip = 0,
        string? search = null, CancellationToken token = default)
    {
        var data = Context.GameEvents.Where(e => e.GameId == gameId);

        if (hideContainer)
            data = data.Where(e => e.Type != EventType.ContainerStart && e.Type != EventType.ContainerDestroy);

        if (!string.IsNullOrWhiteSpace(search))
        {
            data = data.Where(e =>
                (e.Team != null && EF.Functions.Like(e.Team.Name, $"%{search}%")) ||
                (e.User != null && EF.Functions.Like(e.User.UserName, $"%{search}%")) ||
                (e.Values != null && e.Values.Any(v => v != null && EF.Functions.Like(v, $"%{search}%"))));
        }

        return data.OrderByDescending(e => e.PublishTimeUtc).Skip(skip).Take(count).ToArrayAsync(token);
    }
}
