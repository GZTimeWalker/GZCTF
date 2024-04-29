using GZCTF.Extensions;
using GZCTF.Hubs;
using GZCTF.Hubs.Clients;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace GZCTF.Repositories;

public class GameNoticeRepository(
    IDistributedCache cache,
    ILogger<GameNoticeRepository> logger,
    IHubContext<UserHub, IUserClient> hub,
    AppDbContext context) : RepositoryBase(context), IGameNoticeRepository
{
    public async Task<GameNotice> AddNotice(GameNotice notice, CancellationToken token = default)
    {
        await Context.AddAsync(notice, token);
        await SaveAsync(token);

        await cache.RemoveAsync(CacheKey.GameNotice(notice.GameId), token);

        await hub.Clients.Group($"Game_{notice.GameId}")
            .ReceivedGameNotice(notice);

        return notice;
    }

    public Task<GameNotice[]> GetNormalNotices(int gameId, CancellationToken token = default) =>
        Context.GameNotices
            .Where(n => n.GameId == gameId && n.Type == NoticeType.Normal)
            .ToArrayAsync(token);

    public Task<GameNotice?> GetNoticeById(int gameId, int noticeId, CancellationToken token = default) =>
        Context.GameNotices.FirstOrDefaultAsync(e => e.Id == noticeId && e.GameId == gameId, token);

    public Task<GameNotice[]>
        GetNotices(int gameId, int count = 100, int skip = 0, CancellationToken token = default) =>
        cache.GetOrCreateAsync(logger, CacheKey.GameNotice(gameId), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            return Context.GameNotices.Where(e => e.GameId == gameId)
                .OrderByDescending(e => e.Type == NoticeType.Normal ? DateTimeOffset.UtcNow : e.PublishTimeUtc)
                .Skip(skip).Take(count).ToArrayAsync(token);
        }, token);

    public async Task RemoveNotice(GameNotice notice, CancellationToken token = default)
    {
        Context.Remove(notice);
        await SaveAsync(token);

        await cache.RemoveAsync(CacheKey.GameNotice(notice.GameId), token);
    }

    public async Task<GameNotice> UpdateNotice(GameNotice notice, CancellationToken token = default)
    {
        notice.PublishTimeUtc = DateTimeOffset.UtcNow;
        await SaveAsync(token);

        await cache.RemoveAsync(CacheKey.GameNotice(notice.GameId), token);

        return notice;
    }
}
