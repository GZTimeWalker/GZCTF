using GZCTF.Models.Data.Cyctf;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class DivisionExtensionRepository(AppDbContext context) : RepositoryBase(context), IDivisionExtensionRepository
{
    public Task<DivisionExtension?> GetDivisionExtensionByDivisionId(int divisionId, CancellationToken token = default)
        => Context.DivisionExtensions
            .Include(e => e.Division)
            .FirstOrDefaultAsync(e => e.DivisionId == divisionId && !e.Deleted, token);

    public async Task<DivisionExtension> CreateOrUpdateDivisionExtension(DivisionExtension extension, CancellationToken token = default)
    {
        var existing = await Context.DivisionExtensions
            .FirstOrDefaultAsync(e => e.DivisionId == extension.DivisionId, token);

        if (existing is null)
        {
            extension.CreateTime = DateTimeOffset.UtcNow;
            extension.UpdateTime = DateTimeOffset.UtcNow;
            await Context.DivisionExtensions.AddAsync(extension, token);
        }
        else
        {
            existing.MinTeamSize = extension.MinTeamSize;
            existing.MaxTeamSize = extension.MaxTeamSize;
            existing.RegistrationFields = extension.RegistrationFields;
            existing.UpdateTime = DateTimeOffset.UtcNow;
        }

        await SaveAsync(token);
        return existing ?? extension;
    }

    public async Task<bool> DeleteDivisionExtension(int divisionId, CancellationToken token = default)
    {
        var extension = await Context.DivisionExtensions
            .FirstOrDefaultAsync(e => e.DivisionId == divisionId, token);

        if (extension is null)
            return false;

        extension.Deleted = true;
        extension.UpdateTime = DateTimeOffset.UtcNow;
        await SaveAsync(token);
        return true;
    }

    public Task<bool> HasDivisionExtension(int divisionId, CancellationToken token = default)
        => Context.DivisionExtensions.AnyAsync(e => e.DivisionId == divisionId && !e.Deleted, token);

    public Task<List<DivisionExtension>> GetDivisionExtensionsByGameId(int gameId, CancellationToken token = default)
        => Context.DivisionExtensions
            .Include(e => e.Division)
            .Where(e => e.Division.GameId == gameId && !e.Deleted)
            .ToListAsync(token);
}
