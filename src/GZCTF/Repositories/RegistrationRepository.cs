using GZCTF.Models.Data.Cyctf;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class RegistrationRepository(AppDbContext context) : RepositoryBase(context), IRegistrationRepository
{
    public Task<List<Registration>> GetRegistrationsByGameId(int gameId, CancellationToken token = default)
        => Context.Registrations
            .Include(r => r.Team)
            .Include(r => r.Division)
            .Include(r => r.Reviewer)
            .Where(r => r.GameId == gameId && !r.Deleted)
            .OrderByDescending(r => r.CreateTime)
            .ToListAsync(token);

    public Task<Registration?> GetRegistrationByTeamAndGame(int teamId, int gameId, CancellationToken token = default)
        => Context.Registrations
            .Include(r => r.Team)
            .Include(r => r.Division)
            .Include(r => r.Reviewer)
            .FirstOrDefaultAsync(r => r.TeamId == teamId && r.GameId == gameId && !r.Deleted, token);

    public Task<Registration?> GetRegistrationById(int id, CancellationToken token = default)
        => Context.Registrations
            .Include(r => r.Team)
            .Include(r => r.Division)
            .Include(r => r.Reviewer)
            .FirstOrDefaultAsync(r => r.Id == id && !r.Deleted, token);

    public async Task<Registration> CreateRegistration(Registration registration, CancellationToken token = default)
    {
        registration.CreateTime = DateTimeOffset.UtcNow;
        registration.UpdateTime = DateTimeOffset.UtcNow;
        await Context.Registrations.AddAsync(registration, token);
        await SaveAsync(token);
        return registration;
    }

    public async Task<Registration?> UpdateRegistrationStatus(int id, string status, string? reviewNote, Guid? reviewedBy, CancellationToken token = default)
    {
        var registration = await Context.Registrations.FirstOrDefaultAsync(r => r.Id == id, token);

        if (registration is null)
            return null;

        registration.Status = status;
        registration.ReviewNote = reviewNote;
        registration.ReviewedBy = reviewedBy;
        registration.ReviewedAt = DateTimeOffset.UtcNow;
        registration.UpdateTime = DateTimeOffset.UtcNow;

        await SaveAsync(token);
        return registration;
    }

    public Task<bool> HasRegistration(int teamId, int gameId, CancellationToken token = default)
        => Context.Registrations.AnyAsync(r => r.TeamId == teamId && r.GameId == gameId && !r.Deleted, token);

    public async Task<Dictionary<string, int>> GetRegistrationStats(int gameId, CancellationToken token = default)
    {
        var stats = await Context.Registrations
            .Where(r => r.GameId == gameId && !r.Deleted)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(token);

        return stats.ToDictionary(s => s.Status, s => s.Count);
    }
}
