using GZCTF.Models.Data.Cyctf;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class GameExtensionRepository(AppDbContext context) : RepositoryBase(context), IGameExtensionRepository
{
    public Task<GameExtension?> GetGameExtensionByGameId(int gameId, CancellationToken token = default)
        => Context.GameExtensions
            .Include(e => e.Sponsors.Where(s => !s.Deleted))
            .Include(e => e.Awards.Where(a => !a.Deleted))
            .FirstOrDefaultAsync(e => e.GameId == gameId && !e.Deleted, token);

    public async Task<GameExtension> CreateOrUpdateGameExtension(GameExtension extension, CancellationToken token = default)
    {
        var existing = await Context.GameExtensions
            .FirstOrDefaultAsync(e => e.GameId == extension.GameId, token);

        if (existing is null)
        {
            extension.CreateTime = DateTimeOffset.UtcNow;
            extension.UpdateTime = DateTimeOffset.UtcNow;
            await Context.GameExtensions.AddAsync(extension, token);
        }
        else
        {
            existing.RegistrationStartTime = extension.RegistrationStartTime;
            existing.RegistrationEndTime = extension.RegistrationEndTime;
            existing.MaxTeams = extension.MaxTeams;
            existing.ShowRegistrationCount = extension.ShowRegistrationCount;
            existing.ShowEventTime = extension.ShowEventTime;
            existing.EmailWhitelist = extension.EmailWhitelist;
            existing.Status = extension.Status;
            existing.UpdateTime = DateTimeOffset.UtcNow;
        }

        await SaveAsync(token);
        return existing ?? extension;
    }

    public async Task<bool> DeleteGameExtension(int gameId, CancellationToken token = default)
    {
        var extension = await Context.GameExtensions
            .FirstOrDefaultAsync(e => e.GameId == gameId, token);

        if (extension is null)
            return false;

        extension.Deleted = true;
        extension.UpdateTime = DateTimeOffset.UtcNow;
        await SaveAsync(token);
        return true;
    }

    public Task<bool> HasGameExtension(int gameId, CancellationToken token = default)
        => Context.GameExtensions.AnyAsync(e => e.GameId == gameId && !e.Deleted, token);

    public async Task UpdateCurrentTeams(int gameId, int count, CancellationToken token = default)
    {
        var extension = await Context.GameExtensions
            .FirstOrDefaultAsync(e => e.GameId == gameId, token);

        if (extension is not null)
        {
            extension.CurrentTeams = count;
            extension.UpdateTime = DateTimeOffset.UtcNow;
            await SaveAsync(token);
        }
    }
}
