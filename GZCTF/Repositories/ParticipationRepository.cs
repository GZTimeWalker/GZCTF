using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Repositories;

public class ParticipationRepository : RepositoryBase, IParticipationRepository
{
    private readonly IGameRepository gameRepository;

    public ParticipationRepository(
        IGameRepository _gameRepository,
        AppDbContext _context) : base(_context)
    {
        gameRepository = _gameRepository;
    }

    public async Task<bool> EnsureInstances(Participation part, Game game, CancellationToken token = default)
    {
        await context.Entry(part).Collection(p => p.Challenges).LoadAsync(token);
        await context.Entry(game).Collection(g => g.Challenges).LoadAsync(token);

        bool update = false;

        foreach (var challenge in game.Challenges)
            update |= part.Challenges.Add(challenge);

        await SaveAsync(token);

        return update;
    }

    public Task<Participation?> GetParticipationById(int id, CancellationToken token = default)
        => context.Participations.FirstOrDefaultAsync(p => p.Id == id, token);

    public Task<Participation?> GetParticipation(Team team, Game game, CancellationToken token = default)
        => context.Participations.FirstOrDefaultAsync(e => e.Team == team && e.Game == game, token);

    public Task<Participation?> GetParticipation(UserInfo user, Game game, CancellationToken token = default)
        => context.Participations.FirstOrDefaultAsync(p => p.Members.Any(m => m.Game == game && m.User == user));

    public Task<int> GetParticipationCount(Game game, CancellationToken token = default)
        => context.Participations.Where(p => p.GameId == game.Id).CountAsync(token);

    public Task<Participation[]> GetParticipations(Game game, CancellationToken token = default)
        => context.Participations.Where(p => p.GameId == game.Id)
            .Include(p => p.Team)
            .ThenInclude(t => t.Members)
            .OrderBy(p => p.TeamId).ToArrayAsync(token);

    public Task<bool> CheckRepeatParticipation(UserInfo user, Game game, CancellationToken token = default)
        => context.UserParticipations.Include(p => p.Participation)
            .AnyAsync(p => p.User == user && p.Game == game
                && p.Participation.Status != ParticipationStatus.Denied, token);

    public async Task UpdateParticipationStatus(Participation part, ParticipationStatus status, CancellationToken token = default)
    {
        part.Status = status;

        if (status == ParticipationStatus.Accepted)
        {
            part.Team.Locked = true;

            // will also update participation status, update team lock
            // will call SaveAsync
            if (await EnsureInstances(part, part.Game, token))
                // flush scoreboard when instances are updated
                gameRepository.FlushScoreboard(part.Game.Id);
        }
        // team will unlock automatically when request occur
        else
            await SaveAsync(token);
    }

    public async Task RemoveUserParticipations(UserInfo user, Game game, CancellationToken token = default)
        => context.RemoveRange(await context.UserParticipations
                .Where(p => p.User == user && p.Game == game).ToArrayAsync(token));
}