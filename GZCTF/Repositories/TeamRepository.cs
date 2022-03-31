using CTFServer.Models;
using CTFServer.Models.Request.Teams;
using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Repositories;

public class TeamRepository : RepositoryBase, ITeamRepository
{
    public TeamRepository(AppDbContext context) : base(context) { }

    public async Task<Team?> CreateTeam(TeamUpdateModel model, UserInfo user, CancellationToken token = default)
    {
        if (model.Name is null)
            return null;

        Team team = new() { Name = model.Name, Captain = user, Bio = model.Bio };

        team.Members.Add(user);

        await context.AddAsync(team, token);
        await context.SaveChangesAsync(token);

        return team;
    }

    public Task<Team?> GetTeamById(int id, CancellationToken token = default)
        => context.Teams.Include(e => e.Members).FirstOrDefaultAsync(t => t.Id == id, token);

    public Task<int> UpdateAsync(Team team, CancellationToken token = default)
    {
        context.Update(team);
        return context.SaveChangesAsync(token);
    }

    public async Task<bool> VeifyToken(int id, string inviteToken, CancellationToken token = default)
    {
        var team = await context.Teams.FirstOrDefaultAsync(t => t.Id == id, token);
        return team is not null && team.InviteToken == inviteToken;
    }
}
