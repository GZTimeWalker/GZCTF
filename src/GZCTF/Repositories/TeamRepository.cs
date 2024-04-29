using GZCTF.Models.Request.Info;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class TeamRepository(AppDbContext context) : RepositoryBase(context), ITeamRepository
{
    public async Task<bool> AnyActiveGame(Team team, CancellationToken token = default)
    {
        DateTimeOffset current = DateTimeOffset.UtcNow;
        var result = await Context.Participations
            .Where(p => p.Team == team && p.Game.EndTimeUtc > current)
            .AnyAsync(token);

        if (!team.Locked || result)
            return result;

        team.Locked = false;
        await SaveAsync(token);

        return result;
    }

    public override Task<int> CountAsync(CancellationToken token = default) => Context.Teams.CountAsync(token);

    public Task<bool> CheckIsCaptain(UserInfo user, CancellationToken token = default) =>
        Context.Teams.AnyAsync(t => t.Captain == user, token);

    public async Task<Team> CreateTeam(TeamUpdateModel model, UserInfo user, CancellationToken token = default)
    {
        if (model.Name is null)
            throw new ArgumentNullException(nameof(model.Name).ToLower());

        Team team = new() { Name = model.Name, Captain = user, Bio = model.Bio };

        team.Members.Add(user);

        await Context.AddAsync(team, token);
        await SaveAsync(token);

        return team;
    }

    public Task DeleteTeam(Team team, CancellationToken token = default)
    {
        Context.Remove(team);
        return SaveAsync(token);
    }

    public Task<Team?> GetTeamById(int id, CancellationToken token = default) =>
        Context.Teams.Include(e => e.Members).FirstOrDefaultAsync(t => t.Id == id, token);

    public Task<Team[]> GetTeams(int count = 100, int skip = 0, CancellationToken token = default) =>
        Context.Teams.Include(t => t.Members).OrderBy(t => t.Id)
            .Skip(skip).Take(count).ToArrayAsync(token);

    public Task<Team[]> GetUserTeams(UserInfo user, CancellationToken token = default) =>
        Context.Teams.Where(t => t.Members.Any(u => u.Id == user.Id))
            .Include(t => t.Members).ToArrayAsync(token);

    public Task<Team[]> SearchTeams(string hint, CancellationToken token = default)
    {
        var loweredHint = hint.ToLower();
        IQueryable<Team> query = int.TryParse(hint, out var id)
            ? Context.Teams.Include(t => t.Members)
                .Where(item => item.Name.ToLower().Contains(loweredHint) || item.Id == id)
            : Context.Teams.Include(t => t.Members).Where(item => item.Name.ToLower().Contains(loweredHint));

        return query.OrderBy(t => t.Id).Take(30).ToArrayAsync(token);
    }

    public Task Transfer(Team team, UserInfo user, CancellationToken token = default)
    {
        team.Captain = user;
        return SaveAsync(token);
    }

    public async Task<bool> VerifyToken(int id, string inviteCode, CancellationToken token = default)
    {
        Team? team = await Context.Teams.FirstOrDefaultAsync(t => t.Id == id, token);
        return team is not null && team.InviteCode == inviteCode;
    }
}
