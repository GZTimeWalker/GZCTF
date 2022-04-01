using CTFServer.Models;
using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Repositories;

public class NoticeRepository : RepositoryBase, INoticeRepository
{
    public NoticeRepository(AppDbContext _context) : base(_context) { }

    public async Task<Notice> AddNotice(Notice notice, CancellationToken token = default)
    {
        await context.AddAsync(notice, token);
        await context.SaveChangesAsync(token);
        return notice;
    }

    public Task<List<Notice>> GetNotices(CancellationToken token = default)
        => context.Notices.OrderByDescending(e => e.PublishTimeUTC)
            .OrderByDescending(e => e.IsPinned).Take(20).ToListAsync(token);

    public async Task RemoveNotice(Notice notice, CancellationToken token = default)
    {
        context.Remove(notice);
        await context.SaveChangesAsync(token);
    }
}
