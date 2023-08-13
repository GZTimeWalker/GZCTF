using GZCTF.Models.Request.Admin;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ParticipationRepository : RepositoryBase, IParticipationRepository
{
    private readonly IGameRepository _gameRepository;

    public ParticipationRepository(
        IGameRepository gameRepository,
        AppDbContext context) : base(context)
    {
        _gameRepository = gameRepository;
    }

    public async Task<bool> EnsureInstances(Participation part, Game game, CancellationToken token = default)
    {
        var challenges = await _context.Challenges.Where(c => c.Game == game && c.IsEnabled).ToArrayAsync(token);

        // requery instead of Entry
        part = await _context.Participations.Include(p => p.Challenges).SingleAsync(p => p.Id == part.Id, token);

        bool update = false;

        foreach (var challenge in challenges)
            update |= part.Challenges.Add(challenge);

        await SaveAsync(token);

        return update;
    }

    public Task<Participation?> GetParticipationById(int id, CancellationToken token = default)
        => _context.Participations.FirstOrDefaultAsync(p => p.Id == id, token);

    public Task<Participation?> GetParticipation(Team team, Game game, CancellationToken token = default)
        => _context.Participations.FirstOrDefaultAsync(e => e.Team == team && e.Game == game, token);

    public Task<Participation?> GetParticipation(UserInfo user, Game game, CancellationToken token = default)
        => _context.Participations.FirstOrDefaultAsync(p => p.Members.Any(m => m.Game == game && m.User == user), token);

    public Task<int> GetParticipationCount(Game game, CancellationToken token = default)
        => _context.Participations.Where(p => p.GameId == game.Id).CountAsync(token);

    public Task<Participation[]> GetParticipations(Game game, CancellationToken token = default)
        => _context.Participations.Where(p => p.GameId == game.Id)
            .Include(p => p.Team)
            .ThenInclude(t => t.Members)
            .OrderBy(p => p.TeamId).ToArrayAsync(token);

    public Task<WriteupInfoModel[]> GetWriteups(Game game, CancellationToken token = default)
        => _context.Participations.Where(p => p.Game == game && p.Writeup != null)
                .OrderByDescending(p => p.Writeup!.UploadTimeUTC)
                .Select(p => WriteupInfoModel.FromParticipation(p)!)
                .ToArrayAsync(token);

    public Task<bool> CheckRepeatParticipation(UserInfo user, Game game, CancellationToken token = default)
        => _context.UserParticipations.Include(p => p.Participation)
            .AnyAsync(p => p.User == user && p.Game == game
                && p.Participation.Status != ParticipationStatus.Rejected, token);

    public async Task UpdateParticipationStatus(Participation part, ParticipationStatus status, CancellationToken token = default)
    {
        var oldStatus = part.Status;
        part.Status = status;

        if (status == ParticipationStatus.Accepted)
        {
            part.Team.Locked = true;

            // will also update participation status, update team lock
            // will call SaveAsync
            // also flush scoreboard when a team is re-accepted
            if (await EnsureInstances(part, part.Game, token) || oldStatus == ParticipationStatus.Suspended)
                // flush scoreboard when instances are updated
                await _gameRepository.FlushScoreboardCache(part.Game.Id, token);

            return;
        }

        // team will unlock automatically when request occur
        await SaveAsync(token);

        // flush scoreboard when a team is suspended
        if (status == ParticipationStatus.Suspended && part.Game.IsActive)
            await _gameRepository.FlushScoreboardCache(part.GameId, token);
    }

    public async Task RemoveUserParticipations(UserInfo user, Game game, CancellationToken token = default)
        => _context.RemoveRange(await _context.UserParticipations
                .Where(p => p.User == user && p.Game == game).ToArrayAsync(token));

    public async Task RemoveUserParticipations(UserInfo user, Team team, CancellationToken token = default)
        => _context.RemoveRange(await _context.UserParticipations
                .Where(p => p.User == user && p.Team == team).ToArrayAsync(token));

    public Task RemoveParticipation(Participation part, CancellationToken token = default)
    {
        _context.Remove(part);
        return SaveAsync(token);
    }
}
