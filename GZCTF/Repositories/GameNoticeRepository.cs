using CTFServer.Models;
using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Repositories;

public class GameNoticeRepository : RepositoryBase, IGameNoticeRepository
{
    public GameNoticeRepository(AppDbContext _context) : base(_context) { }

    public async Task<GameNotice> AddNotice(Game game, GameNotice notice, CancellationToken token = default)
    {
        await context.Entry(game).Collection(e => e.GameNotices).LoadAsync(token);
        game.GameNotices.Add(notice);
        await context.SaveChangesAsync(token);
        return notice;
    }

    public Task<GameNotice[]> GetNotices(Game game, int count = 10, int skip = 0, CancellationToken token = default)
        => context.Entry(game).Collection(e => e.GameNotices).Query()
            .OrderByDescending(e => e.PublishTimeUTC).Skip(skip).Take(count).ToArrayAsync(token);
}
