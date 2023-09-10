﻿using GZCTF.Models.Request.Admin;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ParticipationRepository(
    CacheHelper cacheHelper,
    IFileRepository fileRepository,
    AppDbContext context) : RepositoryBase(context), IParticipationRepository
{
    public async Task<bool> EnsureInstances(Participation part, Game game, CancellationToken token = default)
    {
        Challenge[] challenges = await context.Challenges.Where(c => c.Game == game && c.IsEnabled).ToArrayAsync(token);

        // requery instead of Entry
        part = await context.Participations.Include(p => p.Challenges).SingleAsync(p => p.Id == part.Id, token);

        var update = false;

        foreach (Challenge challenge in challenges)
            update |= part.Challenges.Add(challenge);

        await SaveAsync(token);

        return update;
    }

    public Task<Participation?> GetParticipationById(int id, CancellationToken token = default) =>
        context.Participations.FirstOrDefaultAsync(p => p.Id == id, token);

    public Task<Participation?> GetParticipation(Team team, Game game, CancellationToken token = default) =>
        context.Participations.FirstOrDefaultAsync(e => e.Team == team && e.Game == game, token);

    public Task<Participation?> GetParticipation(UserInfo user, Game game, CancellationToken token = default) =>
        context.Participations.FirstOrDefaultAsync(p => p.Members.Any(m => m.Game == game && m.User == user),
            token);

    public Task<int> GetParticipationCount(Game game, CancellationToken token = default) =>
        context.Participations.Where(p => p.GameId == game.Id).CountAsync(token);

    public Task<Participation[]> GetParticipations(Game game, CancellationToken token = default) =>
        context.Participations.Where(p => p.GameId == game.Id)
            .Include(p => p.Team)
            .ThenInclude(t => t.Members)
            .OrderBy(p => p.TeamId).ToArrayAsync(token);

    public Task<WriteupInfoModel[]> GetWriteups(Game game, CancellationToken token = default) =>
        context.Participations.Where(p => p.Game == game && p.Writeup != null)
            .OrderByDescending(p => p.Writeup!.UploadTimeUTC)
            .Select(p => WriteupInfoModel.FromParticipation(p)!)
            .ToArrayAsync(token);

    public Task<bool> CheckRepeatParticipation(UserInfo user, Game game, CancellationToken token = default) =>
        context.UserParticipations.Include(p => p.Participation)
            .AnyAsync(p => p.User == user && p.Game == game
                                          && p.Participation.Status != ParticipationStatus.Rejected, token);

    public async Task UpdateParticipationStatus(Participation part, ParticipationStatus status,
        CancellationToken token = default)
    {
        ParticipationStatus oldStatus = part.Status;
        part.Status = status;

        if (status == ParticipationStatus.Accepted)
        {
            part.Team.Locked = true;

            // will also update participation status, update team lock
            // will call SaveAsync
            // also flush scoreboard when a team is re-accepted
            if (await EnsureInstances(part, part.Game, token) || oldStatus == ParticipationStatus.Suspended)
                // flush scoreboard when instances are updated
                await cacheHelper.FlushScoreboardCache(part.Game.Id, token);

            return;
        }

        // team will unlock automatically when request occur
        await SaveAsync(token);

        // flush scoreboard when a team is suspended
        if (status == ParticipationStatus.Suspended && part.Game.IsActive)
            await cacheHelper.FlushScoreboardCache(part.GameId, token);
    }

    public Task<Participation[]> GetParticipationsByIds(IEnumerable<int> ids, CancellationToken token = default) =>
        context.Participations.Where(p => ids.Contains(p.Id))
            .Include(p => p.Team)
            .ToArrayAsync(token);

    public async Task RemoveUserParticipations(UserInfo user, Game game, CancellationToken token = default) =>
        context.RemoveRange(await context.UserParticipations
            .Where(p => p.User == user && p.Game == game).ToArrayAsync(token));

    public async Task RemoveUserParticipations(UserInfo user, Team team, CancellationToken token = default) =>
        context.RemoveRange(await context.UserParticipations
            .Where(p => p.User == user && p.Team == team).ToArrayAsync(token));

    public async Task RemoveParticipation(Participation part, CancellationToken token = default)
    {
        await DeleteParticipationWriteUp(part, token);

        context.Remove(part);
        await SaveAsync(token);
    }

    public Task DeleteParticipationWriteUp(Participation part, CancellationToken token = default)
    {
        if (part.Writeup is not null)
            return fileRepository.DeleteFile(part.Writeup, token);
        return Task.CompletedTask;
    }
}