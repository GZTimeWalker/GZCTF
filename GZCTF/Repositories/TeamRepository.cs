using CTFServer.Models.Request.Info;
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
        var current = DateTimeOffset.UtcNow;
        var result = await context.Participations
            .Where(p => p.Team == team && p.Game.EndTimeUTC > current)
            .AnyAsync(token);

        if (team.Locked && !result)
        {
            team.Locked = false;
            await SaveAsync(token);
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
        await SaveAsync(token);

        return team;
    }

    public Task DeleteTeam(Team team, CancellationToken token = default)
    {
        context.Remove(team);
        return SaveAsync(token);
    }

    public Task<Team?> GetTeamById(int id, CancellationToken token = default)
        => context.Teams.Include(e => e.Members).FirstOrDefaultAsync(t => t.Id == id, token);

    public Task<Team[]> GetTeams(int count = 100, int skip = 0, CancellationToken token = default)
        => context.Teams.Include(t => t.Members).OrderBy(t => t.Id)
            .Skip(skip).Take(count).ToArrayAsync(token);

    public Task<Team[]> GetUserTeams(UserInfo user, CancellationToken token = default)
        => context.Teams.Where(t => t.Members.Any(u => u.Id == user.Id))
            .Include(t => t.Members).ToArrayAsync(token);

    public Task<Team[]> SearchTeams(string hint, CancellationToken token = default)
        => context.Teams.Include(t => t.Members).Where(item => EF.Functions.Like(item.Name, $"%{hint}%"))
            .OrderBy(t => t.Id).Take(30).ToArrayAsync(token);

    public Task Transfer(Team team, UserInfo user, CancellationToken token = default)
    {
        team.Captain = user;
        return SaveAsync(token);
    }

    public async Task<bool> VeifyToken(int id, string inviteCode, CancellationToken token = default)
    {
        var team = await context.Teams.FirstOrDefaultAsync(t => t.Id == id, token);
        return team is not null && team.InviteCode == inviteCode;
    }
}