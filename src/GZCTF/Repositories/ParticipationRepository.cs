using GZCTF.Models.Request.Admin;
using GZCTF.Models.Request.Game;
using GZCTF.Models.Request.Info;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ParticipationRepository(
    CacheHelper cacheHelper,
    IBlobRepository blobRepository,
    IDivisionRepository divisionRepository,
    AppDbContext context) : RepositoryBase(context), IParticipationRepository
{
    public async Task<bool> EnsureInstances(Participation part, Game game, CancellationToken token = default)
    {
        var newInstances = Context.GameChallenges
            .Where(c => c.GameId == game.Id && c.IsEnabled && !Context.Set<GameInstance>()
                .Where(gi => gi.ParticipationId == part.Id)
                .Select(gi => gi.ChallengeId).Contains(c.Id)
            )
            .Select(c => new GameInstance { ParticipationId = part.Id, ChallengeId = c.Id })
            .ToList();

        if (newInstances.Count == 0)
            return false;

        await Context.Set<GameInstance>().AddRangeAsync(newInstances, token);
        await SaveAsync(token);

        return true;
    }

    public Task<Participation?> GetParticipationById(int id, CancellationToken token = default) =>
        Context.Participations.Include(p => p.Division)
            .FirstOrDefaultAsync(p => p.Id == id, token);

    public Task<Participation?> GetParticipation(Team team, Game game, CancellationToken token = default) =>
        Context.Participations.FirstOrDefaultAsync(e => e.Team == team && e.Game == game, token);

    public Task<Participation?> GetParticipation(Guid userId, int gameId, CancellationToken token = default) =>
        Context.Participations
            .FirstOrDefaultAsync(p => p.Members.Any(m => m.GameId == gameId && m.UserId == userId),
                token);

    public Task<int> GetParticipationCount(int gameId, CancellationToken token = default) =>
        Context.Participations.Where(p => p.GameId == gameId).CountAsync(token);

    public Task<Participation[]> GetParticipations(Game game, CancellationToken token = default) =>
        Context.Participations.Where(p => p.GameId == game.Id)
            .Include(p => p.Team)
            .ThenInclude(t => t.Members)
            .OrderBy(p => p.TeamId).ToArrayAsync(token);

    public Task<JoinedTeam[]> GetJoinedTeams(Game game, UserInfo user, CancellationToken token = default) =>
        Context.Participations
            .Where(p => p.Game == game && p.DivisionId != null && p.Team.Members.Any(m => m == user))
            .Select(p => new JoinedTeam { TeamId = p.TeamId, DivisionId = p.DivisionId })
            .ToArrayAsync(token);

    public async Task<WriteupInfoModel> GetWriteups(Game game, CancellationToken token = default)
    {
        var divisions = await Context.Divisions
            .Where(d => d.GameId == game.Id)
            .ToDictionaryAsync(d => d.Id, d => d.Name, token);

        var writeups = await Context.Participations.AsNoTracking()
            .Where(p => p.Game == game && p.Writeup != null)
            .OrderByDescending(p => p.Writeup!.UploadTimeUtc)
            .AsSplitQuery()
            .Select(part => new WriteupInfo
            {
                Id = part.Id,
                Team = TeamInfoModel.FromTeam(part.Team, false),
                File = part.Writeup!,
                Url = part.Writeup!.Url(null),
                DivisionId = part.DivisionId,
                UploadTimeUtc = part.Writeup.UploadTimeUtc
            })
            .ToListAsync(token);

        return new WriteupInfoModel { Divisions = divisions, Writeups = writeups };
    }


    public Task<bool> CheckRepeatParticipation(UserInfo user, Game game, CancellationToken token = default) =>
        Context.UserParticipations.Include(p => p.Participation)
            .AnyAsync(p => p.User == user && p.Game == game
                                          && p.Participation.Status != ParticipationStatus.Rejected, token);

    public async Task UpdateParticipation(Participation part, ParticipationEditModel model,
        CancellationToken token = default)
    {
        await UpdateDivision(part, model.DivisionId, token);

        if (model.Status is { } status)
            await UpdateParticipationStatus(part, status, token);
    }

    public async Task UpdateParticipationStatus(Participation part, ParticipationStatus status,
        CancellationToken token = default)
    {
        if (status == part.Status)
            return;

        part.Status = status;

        switch (status)
        {
            case ParticipationStatus.Accepted:
                // lock team when accepted
                part.Team.Locked = true;
                await EnsureInstances(part, part.Game, token);
                break;
            case ParticipationStatus.Rejected:
                // clear division when rejected
                part.Division = null;
                part.DivisionId = null;
                break;
        }

        await SaveAsync(token);
        // always flush scoreboard, it's inexpensive
        await cacheHelper.FlushScoreboardCache(part.GameId, token);
    }

    async Task UpdateDivision(Participation part, int? divisionId, CancellationToken token = default)
    {
        if (part.DivisionId == divisionId)
            return;

        if (divisionId is { } divId)
        {
            var div = await divisionRepository.GetDivision(part.GameId, divId, token);
            if (div is null)
                return;

            part.DivisionId = divisionId;
            part.Division = div;
        }
        else
        {
            part.DivisionId = null;
            part.Division = null;
        }

        await SaveAsync(token);
        await cacheHelper.FlushScoreboardCache(part.GameId, token);
    }

    public Task<Participation[]> GetParticipationsByIds(IEnumerable<int> ids, CancellationToken token = default) =>
        Context.Participations.Where(p => ids.Contains(p.Id))
            .Include(p => p.Team)
            .Include(p => p.Division)
            .ToArrayAsync(token);

    public async Task RemoveUserParticipations(UserInfo user, Game game, CancellationToken token = default) =>
        Context.RemoveRange(await Context.UserParticipations
            .Where(p => p.User == user && p.Game == game).ToArrayAsync(token));

    public async Task RemoveUserParticipations(UserInfo user, Team team, CancellationToken token = default) =>
        Context.RemoveRange(await Context.UserParticipations
            .Where(p => p.User == user && p.Team == team).ToArrayAsync(token));

    public async Task RemoveParticipation(Participation part, bool save = true, CancellationToken token = default)
    {
        await DeleteParticipationWriteUp(part, token);

        Context.Remove(part);
        if (save)
            await SaveAsync(token);
    }

    public Task DeleteParticipationWriteUp(Participation part, CancellationToken token = default)
    {
        if (part.Writeup is not null)
            return blobRepository.DeleteBlob(part.Writeup, token);
        return Task.CompletedTask;
    }
}
