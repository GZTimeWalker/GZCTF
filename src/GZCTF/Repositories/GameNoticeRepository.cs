using GZCTF.Hubs;
using GZCTF.Hubs.Clients;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace GZCTF.Repositories;

public class GameNoticeRepository : RepositoryBase, IGameNoticeRepository
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<GameNoticeRepository> _logger;
    private readonly IHubContext<UserHub, IUserClient> _hubContext;

    public GameNoticeRepository(IDistributedCache cache,
        ILogger<GameNoticeRepository> logger,
        IHubContext<UserHub, IUserClient> hub,
        AppDbContext context) : base(context)
    {
        _cache = cache;
        _logger = logger;
        _hubContext = hub;
    }

    public async Task<GameNotice> AddNotice(GameNotice notice, CancellationToken token = default)
    {
        await _context.AddAsync(notice, token);
        await SaveAsync(token);

        _cache.Remove(CacheKey.GameNotice(notice.GameId));

        await _hubContext.Clients.Group($"Game_{notice.GameId}")
            .ReceivedGameNotice(notice);

        return notice;
    }

    public Task<GameNotice[]> GetNormalNotices(int gameId, CancellationToken token = default)
        => _context.GameNotices
            .Where(n => n.GameId == gameId && n.Type == NoticeType.Normal)
            .ToArrayAsync(token);

    public Task<GameNotice?> GetNoticeById(int gameId, int noticeId, CancellationToken token = default)
        => _context.GameNotices.FirstOrDefaultAsync(e => e.Id == noticeId && e.GameId == gameId, token);

    public Task<GameNotice[]> GetNotices(int gameId, int count = 100, int skip = 0, CancellationToken token = default)
        => _cache.GetOrCreateAsync(_logger, CacheKey.GameNotice(gameId), (entry) =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            return _context.GameNotices.Where(e => e.GameId == gameId)
                .OrderByDescending(e => e.Type == NoticeType.Normal ? DateTimeOffset.UtcNow : e.PublishTimeUTC)
                .Skip(skip).Take(count).ToArrayAsync(token);
        }, token);

    public Task RemoveNotice(GameNotice notice, CancellationToken token = default)
    {
        _context.Remove(notice);
        _cache.Remove(CacheKey.GameNotice(notice.GameId));

        return SaveAsync(token);
    }

    public async Task<GameNotice> UpdateNotice(GameNotice notice, CancellationToken token = default)
    {
        notice.PublishTimeUTC = DateTimeOffset.UtcNow;
        await SaveAsync(token);

        _cache.Remove(CacheKey.GameNotice(notice.GameId));

        return notice;
    }
}
