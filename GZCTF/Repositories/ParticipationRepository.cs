using CTFServer.Models;
using CTFServer.Repositories.Interface;

namespace CTFServer.Repositories;

public class ParticipationRepository : RepositoryBase, IParticipationRepository
{
    public ParticipationRepository(AppDbContext _context) : base(_context) { }

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
}
