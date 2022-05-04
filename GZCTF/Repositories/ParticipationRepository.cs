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
        => context.Participations.Include(e => e.Team).Include(e => e.Game)
            .FirstOrDefaultAsync(e => e.Team == team && e.Game == game, token);
}