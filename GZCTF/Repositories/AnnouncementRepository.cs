
using Microsoft.EntityFrameworkCore;
using CTFServer.Models;
using CTFServer.Repositories.Interface;

namespace CTFServer.Repositories;
public class AnnouncementRepository : RepositoryBase, IAnnouncementRepository
{
    public AnnouncementRepository(AppDbContext context) : base(context)
    { 
    }

    public async Task<Announcement> AddOrUpdateAnnouncement(Announcement announcement, CancellationToken token)
    {
        if (announcement.Id == -1)
            await context.Announcements.AddAsync(announcement, token);
        else
            context.Announcements.Update(announcement);

        await context.SaveChangesAsync(token);
        return announcement;
    }

    public Task<Announcement?> GetAnnouncementById(int Id, CancellationToken token)
        => context.Announcements.FirstOrDefaultAsync(x => x.Id == Id, token);

    public async Task<List<Announcement>> GetAnnouncements(int skip, int count, CancellationToken token)
    {
        var res = await context.Announcements.Where(x => x.IsPinned).ToListAsync(token);

        res.AddRange(await context.Announcements.Where(x => !x.IsPinned).OrderByDescending(x => x.PublishTimeUTC).Skip(skip).Take(count).ToListAsync(token));

        return res;
    }

    public async Task RemoveAnnouncement(Announcement announcement, CancellationToken token)
    {
        context.Announcements.Remove(announcement);
        await context.SaveChangesAsync(token);
    }
}
