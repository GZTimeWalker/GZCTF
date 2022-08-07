using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CTFServer.Repositories;

public class NoticeRepository : RepositoryBase, INoticeRepository
{
    private readonly IMemoryCache cache;

    public NoticeRepository(IMemoryCache memoryCache, AppDbContext _context) : base(_context)
    {
        cache = memoryCache;
    }

    public async Task<Notice> CreateNotice(Notice notice, CancellationToken token = default)
    {
        await context.AddAsync(notice, token);
        await SaveAsync(token);

        cache.Remove(CacheKey.Notices);

        return notice;
    }

    public Task<Notice[]> GetLatestNotices(CancellationToken token = default)
        => cache.GetOrCreateAsync(CacheKey.Notices, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
            return context.Notices.OrderByDescending(n => n.IsPinned)
                    .ThenByDescending(n => n.PublishTimeUTC).Take(3).ToArrayAsync(token);
        });

    public Task<Notice?> GetNoticeById(int id, CancellationToken token = default)
        => context.Notices.FirstOrDefaultAsync(notice => notice.Id == id, token);

    public Task<Notice[]> GetNotices(CancellationToken token = default)
        => context.Notices.OrderByDescending(e => e.PublishTimeUTC)
            .OrderByDescending(e => e.IsPinned).Take(20).ToArrayAsync(token);

    public async Task RemoveNotice(Notice notice, CancellationToken token = default)
    {
        context.Remove(notice);
        await SaveAsync(token);

        cache.Remove(CacheKey.Notices);
    }

    public async Task UpdateNotice(Notice notice, CancellationToken token = default)
    {
        await SaveAsync(token);
        cache.Remove(CacheKey.Notices);
    }
}