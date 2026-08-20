using GZCTF.Models.Data.Cyctf;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class AwardRepository(AppDbContext context) : RepositoryBase(context), IAwardRepository
{
    public Task<List<Award>> GetAwardsByGameId(int gameId, CancellationToken token = default)
        => Context.Awards
            .Where(a => a.GameId == gameId && !a.Deleted)
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.Id)
            .ToListAsync(token);

    public Task<Award?> GetAwardById(int id, CancellationToken token = default)
        => Context.Awards.FirstOrDefaultAsync(a => a.Id == id && !a.Deleted, token);

    public async Task<Award> CreateAward(Award award, CancellationToken token = default)
    {
        award.CreateTime = DateTimeOffset.UtcNow;
        award.UpdateTime = DateTimeOffset.UtcNow;
        await Context.Awards.AddAsync(award, token);
        await SaveAsync(token);
        return award;
    }

    public async Task<Award?> UpdateAward(Award award, CancellationToken token = default)
    {
        var existing = await Context.Awards.FirstOrDefaultAsync(a => a.Id == award.Id, token);

        if (existing is null)
            return null;

        existing.Name = award.Name;
        existing.Description = award.Description;
        existing.PrimaryColor = award.PrimaryColor;
        existing.SecondaryColor = award.SecondaryColor;
        existing.SortOrder = award.SortOrder;
        existing.UpdateTime = DateTimeOffset.UtcNow;

        await SaveAsync(token);
        return existing;
    }

    public async Task<bool> DeleteAward(int id, CancellationToken token = default)
    {
        var award = await Context.Awards.FirstOrDefaultAsync(a => a.Id == id, token);

        if (award is null)
            return false;

        award.Deleted = true;
        award.UpdateTime = DateTimeOffset.UtcNow;
        await SaveAsync(token);
        return true;
    }
}
