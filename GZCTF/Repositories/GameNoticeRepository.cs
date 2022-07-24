using CTFServer.Hubs;
using CTFServer.Hubs.Clients;
using CTFServer.Repositories.Interface;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CTFServer.Repositories;

public class GameNoticeRepository : RepositoryBase, IGameNoticeRepository
{
    private readonly IMemoryCache cache;
    private IHubContext<UserHub, IUserClient> hubContext;

    public GameNoticeRepository(IMemoryCache memoryCache,
        IHubContext<UserHub, IUserClient> hub,
        AppDbContext _context) : base(_context)
    {
        cache = memoryCache;
        hubContext = hub;
    }

    public async Task<GameNotice> CreateNotice(Game game, GameNotice notice, CancellationToken token = default)
    {
        await context.Entry(game).Collection(e => e.GameNotices).LoadAsync(token);

        game.GameNotices.Add(notice);
        await context.SaveChangesAsync(token);

        cache.Remove(CacheKey.GameNotice(game.Id));

        await hubContext.Clients.Group($"Game_{game.Id}").ReceivedGameNotice(notice);

        return notice;
    }

    public Task<GameNotice?> GetNoticeById(int gameId, int noticeId, CancellationToken token = default)
        => context.GameNotices.FirstOrDefaultAsync(e => e.Id == noticeId && e.GameId == gameId, token);

    public Task<GameNotice[]> GetNotices(int gameId, CancellationToken token = default)
        => cache.GetOrCreateAsync(CacheKey.GameNotice(gameId), (entry) =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            return context.GameNotices.Where(e => e.GameId == gameId)
                .OrderByDescending(e => e.PublishTimeUTC)
                .ToArrayAsync(token);
        });

    public Task RemoveNotice(GameNotice notice, CancellationToken token = default)
    {
        context.Remove(notice);
        return context.SaveChangesAsync(token);
    }
}