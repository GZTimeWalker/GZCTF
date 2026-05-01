using GZCTF.Hubs;
using GZCTF.Hubs.Clients;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using GZCTF.Services.Webhook;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class GameNoticeRepository(
    CacheHelper cacheHelper,
    ILogger<GameNoticeRepository> logger,
    IHubContext<UserHub, IUserClient> hub,
    ISendWebhookService webhookService,
    AppDbContext context) : RepositoryBase(context), IGameNoticeRepository
{
    public async Task<GameNotice> AddNotice(GameNotice notice, bool broadcast = true, CancellationToken token = default)
    {
        await Context.AddAsync(notice, token);
        await SaveAsync(token);

        await cacheHelper.RemoveAsync(CacheKey.GameNotice(notice.GameId), token);

        if (broadcast)
        {
            await hub.Clients.Group($"Game_{notice.GameId}").ReceivedGameNotice(notice);

            var game = await Context.Games.FindAsync([notice.GameId], token);
            if (game?.DiscordWebhook is { Length: > 0 } webhookUrl)
                _ = webhookService.SendNoticeAsync(notice, webhookUrl);
        }

        return notice;
    }

    public Task<GameNotice[]> GetNormalNotices(int gameId, CancellationToken token = default) =>
        Context.GameNotices
            .Where(n => n.GameId == gameId && n.Type == NoticeType.Normal)
            .ToArrayAsync(token);

    public Task<GameNotice?> GetNoticeById(int gameId, int noticeId, CancellationToken token = default) =>
        Context.GameNotices.FirstOrDefaultAsync(e => e.Id == noticeId && e.GameId == gameId, token);

    public Task<DataWithModifiedTime<GameNotice[]>> GetLatestNotices(int gameId, CancellationToken token = default)
        => cacheHelper.GetOrCreateAsync(logger, CacheKey.GameNotice(gameId), async entry =>
        {
            var now = DateTimeOffset.UtcNow;

            // Only include Normal notices whose scheduled publish time has arrived
            var notices = await Context.GameNotices
                .Where(e => e.GameId == gameId &&
                            (e.Type != NoticeType.Normal || e.PublishTimeUtc <= now))
                .OrderByDescending(e => e.Type == NoticeType.Normal ? DateTimeOffset.UtcNow : e.PublishTimeUtc)
                .Take(300).ToArrayAsync(token);

            // Expire the cache just in time for the next scheduled notice, max 30 min
            var nextScheduled = await Context.GameNotices
                .Where(e => e.GameId == gameId && e.Type == NoticeType.Normal && e.PublishTimeUtc > now)
                .Select(e => (DateTimeOffset?)e.PublishTimeUtc)
                .MinAsync(token);

            var ttl = nextScheduled is { } next
                ? TimeSpan.FromTicks(Math.Min((next - now).Ticks, TimeSpan.FromMinutes(30).Ticks))
                : TimeSpan.FromMinutes(30);

            entry.AbsoluteExpirationRelativeToNow = ttl;

            return new DataWithModifiedTime<GameNotice[]>(notices, now);
        }, token: token);

    public async Task RemoveNotice(GameNotice notice, CancellationToken token = default)
    {
        Context.Remove(notice);
        await SaveAsync(token);

        await cacheHelper.RemoveAsync(CacheKey.GameNotice(notice.GameId), token);
    }

    public async Task<GameNotice> UpdateNotice(GameNotice notice, CancellationToken token = default)
    {
        await SaveAsync(token);
        await cacheHelper.RemoveAsync(CacheKey.GameNotice(notice.GameId), token);
        return notice;
    }
}
