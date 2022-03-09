
using Microsoft.EntityFrameworkCore;
using CTFServer.Models;
using CTFServer.Repositories.Interface;

namespace CTFServer.Repositories;
public class NoticeRepository : RepositoryBase, INoticeRepository
{
    public NoticeRepository(AppDbContext context) : base(context) { }

    public async Task<Notice> AddOrUpdateNotice(Notice notice, CancellationToken token)
    {
        if (notice.Id == -1)
            await context.Notices.AddAsync(notice, token);
        else
            context.Notices.Update(notice);

        await context.SaveChangesAsync(token);
        return notice;
    }

    public Task<Notice?> GetNoticeById(int Id, CancellationToken token)
        => context.Notices.FirstOrDefaultAsync(x => x.Id == Id, token);

    public async Task<List<Notice>> GetNotices(int skip, int count, CancellationToken token)
    {
        var res = await context.Notices.Where(x => x.IsPinned).ToListAsync(token);

        res.AddRange(await context.Notices.Where(x => !x.IsPinned).OrderByDescending(x => x.PublishTimeUTC).Skip(skip).Take(count).ToListAsync(token));

        return res;
    }

    public async Task RemoveNotice(Notice notice, CancellationToken token)
    {
        context.Notices.Remove(notice);
        await context.SaveChangesAsync(token);
    }
}
