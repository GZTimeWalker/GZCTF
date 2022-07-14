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
        await context.SaveChangesAsync(token);

        return participation;
    }

    public Task<Participation?> GetParticipation(Team team, Game game, CancellationToken token = default)
        => context.Participations.FirstOrDefaultAsync(e => e.Team == team && e.Game == game, token);

    public Task<Participation?> GetParticipationById(int id, CancellationToken token = default)
        => context.Participations.FirstOrDefaultAsync(p => p.Id == id, token);

    public Task<Participation[]> GetParticipations(Game game, int count = 20, int skip = 0, CancellationToken token = default)
        => context.Participations.Where(p => p.GameId == game.Id).OrderBy(p => p.TeamId).Skip(skip).Take(count).ToArrayAsync(token);
}