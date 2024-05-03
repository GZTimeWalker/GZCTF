using GZCTF.Models.Request.Edit;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GZCTF.Repositories;

public class GameChallengeRepository(AppDbContext context,
    IFileRepository fileRepository,
    CacheHelper cacheHelper
    ) : RepositoryBase(context),
    IGameChallengeRepository
{
    public async Task AddFlags(GameChallenge challenge, FlagCreateModel[] models, CancellationToken token = default)
    {
        foreach (FlagCreateModel model in models)
        {
            var attachment = model.ToAttachment(await fileRepository.GetFileByHash(model.FileHash, token));

            challenge.Flags.Add(new() { Flag = model.Flag, Challenge = challenge, Attachment = attachment });
        }

        await SaveAsync(token);
    }

    public async Task<GameChallenge> CreateChallenge(Game game, GameChallenge challenge,
        CancellationToken token = default)
    {
        await Context.AddAsync(challenge, token);
        game.Challenges.Add(challenge);
        await SaveAsync(token);
        return challenge;
    }

    public async Task<bool> EnsureInstances(GameChallenge challenge, Game game, CancellationToken token = default)
    {
        await Context.Entry(challenge).Collection(c => c.Teams).LoadAsync(token);
        await Context.Entry(game).Collection(g => g.Participations).LoadAsync(token);

        var update = game.Participations.Aggregate(false,
            (current, participation) => challenge.Teams.Add(participation) || current);

        await SaveAsync(token);

        return update;
    }

    public Task<GameChallenge?> GetChallenge(int gameId, int id, bool withFlag = false,
        CancellationToken token = default)
    {
        IQueryable<GameChallenge> challenges = Context.GameChallenges
            .Where(c => c.Id == id && c.GameId == gameId);

        if (withFlag)
            challenges = challenges.Include(e => e.Flags);

        return challenges.FirstOrDefaultAsync(token);
    }

    public Task<GameChallenge[]> GetChallenges(int gameId, CancellationToken token = default) =>
        Context.GameChallenges.Where(c => c.GameId == gameId).OrderBy(c => c.Id).ToArrayAsync(token);

    public Task<GameChallenge[]> GetChallengesWithTrafficCapturing(int gameId, CancellationToken token = default) =>
        Context.GameChallenges.IgnoreAutoIncludes().Where(c => c.GameId == gameId && c.EnableTrafficCapture)
            .ToArrayAsync(token);

    public async Task RemoveChallenge(GameChallenge challenge, CancellationToken token = default)
    {
        await DeleteAllAttachment(challenge, true, token);

        Context.Remove(challenge);
        await SaveAsync(token);
    }

    public async Task<TaskStatus> RemoveFlag(GameChallenge challenge, int flagId, CancellationToken token = default)
    {
        FlagContext? flag = challenge.Flags.FirstOrDefault(f => f.Id == flagId);

        if (flag is null)
            return TaskStatus.NotFound;

        await fileRepository.DeleteAttachment(flag.Attachment, token);

        Context.Remove(flag);

        if (challenge.Flags.Count == 0)
            challenge.IsEnabled = false;

        await SaveAsync(token);

        return TaskStatus.Success;
    }

    public async Task<bool> RecalculateAcceptedCount(Game game, CancellationToken token = default)
    {
        IDbContextTransaction trans = await Context.Database.BeginTransactionAsync(token);

        try
        {
            foreach (int cId in await Context.GameChallenges.IgnoreAutoIncludes()
                         .Where(c => c.GameId == game.Id)
                         .Select(c => c.Id)
                         .ToArrayAsync(token))
            {
                var count = await Context.GameInstances
                    .IgnoreAutoIncludes()
                    .Include(i => i.Participation)
                    .Where(i => i.ChallengeId == cId && i.IsSolved &&
                                i.Participation.Status == ParticipationStatus.Accepted)
                    .CountAsync(token);

                var chal = await Context.GameChallenges.IgnoreAutoIncludes()
                    .SingleAsync(c => c.Id == cId, token);

                if (chal.AcceptedCount == count)
                    continue;

                chal.AcceptedCount = count;
                Context.Update(chal);
                await SaveAsync(token);
            }

            await cacheHelper.FlushScoreboardCache(game.Id, token);
            await trans.CommitAsync(token);
        }
        catch
        {
            await trans.RollbackAsync(token);
            return false;
        }

        return true;
    }

    public async Task UpdateAttachment(GameChallenge challenge, AttachmentCreateModel model,
        CancellationToken token = default)
    {
        var attachment = model.ToAttachment(await fileRepository.GetFileByHash(model.FileHash, token));

        await DeleteAllAttachment(challenge, false, token);

        if (attachment is not null)
            await Context.AddAsync(attachment, token);

        challenge.Attachment = attachment;

        await SaveAsync(token);
    }

    internal async Task DeleteAllAttachment(GameChallenge challenge, bool purge = false,
        CancellationToken token = default)
    {
        await fileRepository.DeleteAttachment(challenge.Attachment, token);

        if (purge && challenge.Type == ChallengeType.DynamicAttachment)
        {
            foreach (FlagContext flag in challenge.Flags)
                await fileRepository.DeleteAttachment(flag.Attachment, token);

            Context.RemoveRange(challenge.Flags);
        }
    }
}
