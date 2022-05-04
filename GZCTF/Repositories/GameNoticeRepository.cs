using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Repositories;

public class GameNoticeRepository : RepositoryBase, IGameNoticeRepository
{
    public GameNoticeRepository(AppDbContext _context) : base(_context)
    {
    }

    public async Task<GameNotice> CreateNotice(Game game, GameNotice notice, CancellationToken token = default)
    {
        await context.Entry(game).Collection(e => e.GameNotices).LoadAsync(token);
        game.GameNotices.Add(notice);
        await context.SaveChangesAsync(token);

        // TODO: send to signalR (for user)

        return notice;
    }

    public Task<GameNotice?> GetNoticeById(int gameId, int noticeId, CancellationToken token = default)
        => context.GameNotices.FirstOrDefaultAsync(e => e.Id == noticeId && e.GameId == gameId, token);

    public Task<GameNotice[]> GetNotices(int gameId, int count = 10, int skip = 0, CancellationToken token = default)
        => context.GameNotices.Where(e => e.GameId == gameId)
            .OrderByDescending(e => e.PublishTimeUTC).Skip(skip).Take(count).ToArrayAsync(token);

    public Task RemoveNotice(GameNotice notice, CancellationToken token = default)
    {
        context.Remove(notice);
        return context.SaveChangesAsync(token);
    }
}