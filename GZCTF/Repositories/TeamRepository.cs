using CTFServer.Models;
using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Repositories;

public class TeamRepository : RepositoryBase, ITeamRepository
{
    public TeamRepository(AppDbContext context) : base(context) { }

    public async Task<Team?> CreateTeam(string name, UserInfo user, CancellationToken token = default)
    {
        Team team = new() { Name = name, Captain = user };

        team.Members.Add(user);

        await context.AddAsync(team, token);
        await context.SaveChangesAsync(token);

        return team;
    }

    public Task<Team?> GetTeamById(int id, CancellationToken token = default)
        => context.Teams.FirstOrDefaultAsync(t => t.Id == id, token);

    public Task<List<Team>> GetTeamsByUser(UserInfo user, CancellationToken token = default)
        => context.Teams.Where(e => e.Members.Any(u => u.Id == user.Id)).ToListAsync(token);

    public Task<int> UpdateAsync(Team team, CancellationToken token = default)
    {
        context.Update(team);
        return context.SaveChangesAsync(token);
    }
}
