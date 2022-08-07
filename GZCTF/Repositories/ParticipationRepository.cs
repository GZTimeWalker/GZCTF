using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Repositories;

public class ParticipationRepository : RepositoryBase, IParticipationRepository
{
    public ParticipationRepository(AppDbContext _context) : base(_context)
    {
    }

    public async Task<Participation> CreateParticipation(Team team, Game game, CancellationToken token = default)
    {
        Participation participation = new()
        {
            Game = game,
            Team = team
        };

        await context.AddAsync(participation, token);
        await SaveAsync(token);

        return participation;
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

    public Task<Participation?> GetParticipation(Team team, Game game, CancellationToken token = default)
        => context.Participations.FirstOrDefaultAsync(e => e.Team == team && e.Game == game, token);

    public Task<Participation?> GetParticipationById(int id, CancellationToken token = default)
        => context.Participations.FirstOrDefaultAsync(p => p.Id == id, token);

    public Task<Participation[]> GetParticipations(Game game, CancellationToken token = default)
        => context.Participations.Where(p => p.GameId == game.Id)
            .Include(p => p.Team)
            .ThenInclude(t => t.Members)
            .OrderBy(p => p.TeamId).ToArrayAsync(token);
}