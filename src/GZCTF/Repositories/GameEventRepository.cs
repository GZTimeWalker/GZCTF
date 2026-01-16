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
        IQueryable<GameEvent> data;

        // Use raw SQL for search to handle array searching
        if (!string.IsNullOrWhiteSpace(search))
        {
            if (hideContainer)
            {
                data = Context.GameEvents.FromSqlInterpolated($@"
                    SELECT ge.* FROM ""GameEvents"" ge
                    LEFT JOIN ""Participations"" p ON ge.""TeamId"" = p.""Id""
                    LEFT JOIN ""Teams"" t ON p.""TeamId"" = t.""Id""
                    LEFT JOIN ""AspNetUsers"" u ON ge.""UserId"" = u.""Id""
                    WHERE ge.""GameId"" = {gameId}
                    AND ge.""Type"" != {(int)EventType.ContainerStart}
                    AND ge.""Type"" != {(int)EventType.ContainerDestroy}
                    AND (
                        t.""Name"" ILIKE {"%" + search + "%"}
                        OR u.""UserName"" ILIKE {"%" + search + "%"}
                        OR array_to_string(ge.""Values"", ' ') ILIKE {"%" + search + "%"}
                    )
                    )");
            }
            else
            {
                data = Context.GameEvents.FromSqlInterpolated($@"
                    SELECT ge.* FROM ""GameEvents"" ge
                    LEFT JOIN ""Participations"" p ON ge.""TeamId"" = p.""Id""
                    LEFT JOIN ""Teams"" t ON p.""TeamId"" = t.""Id""
                    LEFT JOIN ""AspNetUsers"" u ON ge.""UserId"" = u.""Id""
                    WHERE ge.""GameId"" = {gameId}
                    AND (
                        t.""Name"" ILIKE {"%" + search + "%"}
                        OR u.""UserName"" ILIKE {"%" + search + "%"}
                        OR ge.""Values"" ILIKE {"%" + search + "%"}
                    )");
            }
        }
        else
        {
            data = Context.GameEvents.Where(e => e.GameId == gameId);

            if (hideContainer)
                data = data.Where(e => e.Type != EventType.ContainerStart && e.Type != EventType.ContainerDestroy);

            // data = data.OrderByDescending(e => e.PublishTimeUtc);
        }

        return data.OrderByDescending(e => e.PublishTimeUtc).Skip(skip).Take(count).ToArrayAsync(token);
    }

    public async Task<bool> IsChallengeOpened(int gameId, int teamId, int challengeId, CancellationToken token = default)
    {
        var cidStr = challengeId.ToString();
        var events = await Context.GameEvents
            .Where(e => e.GameId == gameId && e.TeamId == teamId && e.Type == EventType.ChallengeOpened)
            .Select(e => new { e.Values })
            .ToListAsync(token);

        return events.Any(e => e.Values != null && e.Values.Count > 0 && e.Values[0] == cidStr);
    }
}
