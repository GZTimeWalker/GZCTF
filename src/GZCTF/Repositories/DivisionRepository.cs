using GZCTF.Models.Request.Edit;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class DivisionRepository(AppDbContext context, CacheHelper cacheHelper)
    : RepositoryBase(context), IDivisionRepository
{
    private async ValueTask FlushCache(int gameId, CancellationToken token = default)
    {
        await cacheHelper.RemoveAsync(CacheKey.GameCache(gameId), token);
        await cacheHelper.FlushScoreboardCache(gameId, token);
    }

    public async Task<Division> CreateDivision(Game game, DivisionCreateModel model, CancellationToken token = default)
    {
        // Create base division first to obtain ID
        var division = new Division
        {
            GameId = game.Id,
            Name = model.Name,
            InviteCode = model.InviteCode,
            DefaultPermissions = model.DefaultPermissions ?? GamePermission.All,
        };

        await Context.AddAsync(division, token);

        division.UpdateChallengeConfigs(model.ChallengeConfigs);

        await SaveAsync(token);
        await FlushCache(game.Id, token);

        return division;
    }

    public Task<Division[]> GetDivisions(int gameId, CancellationToken token = default) =>
        Context.Divisions.Where(d => d.GameId == gameId)
            .Include(d => d.ChallengeConfigs)
            .OrderBy(d => d.Id)
            .ToArrayAsync(token);

    public Task<Division?> GetDivision(int gameId, int divisionId, CancellationToken token = default) =>
        Context.Divisions
            .Include(d => d.ChallengeConfigs)
            .FirstOrDefaultAsync(d => d.Id == divisionId && d.GameId == gameId, token);

    public Task<HashSet<int>> GetJoinableDivisionIds(int gameId, CancellationToken token = default) =>
        Context.Divisions
            .Where(d => d.GameId == gameId && d.DefaultPermissions.HasFlag(GamePermission.JoinGame))
            .Select(d => d.Id)
            .ToHashSetAsync(token);

    public async ValueTask<GamePermission> GetPermission(int? divisionId, int? challengeId,
        CancellationToken token = default)
    {
        if (divisionId is null)
            return GamePermission.All;

        var result = await Context.Divisions
            .AsNoTracking()
            .Where(d => d.Id == divisionId)
            .Select(d => new
            {
                d.DefaultPermissions,
                Config = d.ChallengeConfigs.FirstOrDefault(c => c.ChallengeId == challengeId)
            })
            .FirstOrDefaultAsync(token);

        if (result is null)
            return GamePermission.All;

        if (result.Config is { } config)
            return config.Permissions;

        return result.DefaultPermissions;
    }

    public async Task UpdateDivision(Division division, DivisionEditModel model, CancellationToken token = default)
    {
        division.Update(model);
        await SaveAsync(token);
        await FlushCache(division.GameId, token);
    }

    public async Task RemoveDivision(Division division, CancellationToken token = default)
    {
        var gameId = division.GameId;
        Context.Remove(division);
        await SaveAsync(token);
        await FlushCache(gameId, token);
    }
}
