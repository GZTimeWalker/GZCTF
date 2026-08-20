using GZCTF.Models.Data.Cyctf;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class SponsorRepository(AppDbContext context) : RepositoryBase(context), ISponsorRepository
{
    public Task<List<Sponsor>> GetSponsorsByGameId(int gameId, CancellationToken token = default)
        => Context.Sponsors
            .Where(s => s.GameId == gameId && !s.Deleted)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Id)
            .ToListAsync(token);

    public Task<Sponsor?> GetSponsorById(int id, CancellationToken token = default)
        => Context.Sponsors.FirstOrDefaultAsync(s => s.Id == id && !s.Deleted, token);

    public async Task<Sponsor> CreateSponsor(Sponsor sponsor, CancellationToken token = default)
    {
        sponsor.CreateTime = DateTimeOffset.UtcNow;
        sponsor.UpdateTime = DateTimeOffset.UtcNow;
        await Context.Sponsors.AddAsync(sponsor, token);
        await SaveAsync(token);
        return sponsor;
    }

    public async Task<Sponsor?> UpdateSponsor(Sponsor sponsor, CancellationToken token = default)
    {
        var existing = await Context.Sponsors.FirstOrDefaultAsync(s => s.Id == sponsor.Id, token);

        if (existing is null)
            return null;

        existing.ShortName = sponsor.ShortName;
        existing.FullName = sponsor.FullName;
        existing.Website = sponsor.Website;
        existing.LogoUrl = sponsor.LogoUrl;
        existing.Type = sponsor.Type;
        existing.TypeLabel = sponsor.TypeLabel;
        existing.SortOrder = sponsor.SortOrder;
        existing.UpdateTime = DateTimeOffset.UtcNow;

        await SaveAsync(token);
        return existing;
    }

    public async Task<bool> DeleteSponsor(int id, CancellationToken token = default)
    {
        var sponsor = await Context.Sponsors.FirstOrDefaultAsync(s => s.Id == id, token);

        if (sponsor is null)
            return false;

        sponsor.Deleted = true;
        sponsor.UpdateTime = DateTimeOffset.UtcNow;
        await SaveAsync(token);
        return true;
    }
}
