using CTFServer.Models.Request.Teams;
using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Repositories;

public class TeamRepository : RepositoryBase, ITeamRepository
{
    public TeamRepository(AppDbContext _context) : base(_context)
    {
    }

    public async Task<bool> AnyActiveGame(Team team, CancellationToken token = default)
    {
        bool result = await context.Participations.AnyAsync(p => p.Team == team && p.Game.IsActive, token);

        if (team.Locked != result)
        {
            team.Locked = result;
            await context.SaveChangesAsync(token);
        }

        return result;
    }

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

    public Task<int> DeleteTeam(Team team, CancellationToken token = default)
    {
        context.Remove(team);
        return context.SaveChangesAsync(token);
    }

    public async Task<Team?> GetActiveTeamWithMembers(UserInfo user, CancellationToken token = default)
    {
        if (user.ActiveTeamId is null)
            return null;

        await context.Entry(user).Reference(u => u.ActiveTeam).LoadAsync(token);
        await context.Entry(user.ActiveTeam!).Collection(u => u.Members).LoadAsync(token);

        return user.ActiveTeam;
    }

    public Task<Team?> GetTeamById(int id, CancellationToken token = default)
        => context.Teams.Include(e => e.Members).FirstOrDefaultAsync(t => t.Id == id, token);

    public Task<Team[]> GetTeams(int count = 100, int skip = 0, CancellationToken token = default)
        => context.Teams.OrderBy(t => t.Id).Skip(0).Take(count).ToArrayAsync(token);

    public Task<Team[]> GetUserTeams(UserInfo user, CancellationToken token = default)
        => context.Teams.Where(t => t.Members.Any(u => u.Id == user.Id))
            .Include(t => t.Members).ToArrayAsync(token);

    public async Task<bool> VeifyToken(int id, string inviteCode, CancellationToken token = default)
    {
        var team = await context.Teams.FirstOrDefaultAsync(t => t.Id == id, token);
        return team is not null && team.InviteCode == inviteCode;
    }
}