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

    public Task<Notice?> GetNoticeById(int id, CancellationToken token = default)
        => context.Notices.FirstOrDefaultAsync(notice => notice.Id == id, token);

    public Task<Notice[]> GetNotices(CancellationToken token = default)
        => context.Notices.OrderByDescending(e => e.PublishTimeUTC)
            .OrderByDescending(e => e.IsPinned).Take(20).ToArrayAsync(token);

    public async Task RemoveNotice(Notice notice, CancellationToken token = default)
    {
        context.Remove(notice);
        await context.SaveChangesAsync(token);
    }

    public Task<int> UpdateAsync(Notice notice, CancellationToken token = default)
    {
        context.Update(notice);
        return context.SaveChangesAsync(token);
    }
}
