using GZCTF.Models.Request.Edit;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class GameChallengeRepository(
    AppDbContext context,
    IBlobRepository blobRepository,
    CacheHelper cacheHelper
) : RepositoryBase(context),
    IGameChallengeRepository
{
    public async Task AddFlags(GameChallenge challenge, FlagCreateModel[] models, CancellationToken token = default)
    {
        foreach (var model in models)
        {
            var attachment = model.ToAttachment(await blobRepository.GetBlobByHash(model.FileHash, token));

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

    public Task<GameChallenge?> GetChallenge(int gameId, int id, CancellationToken token = default)
        => Context.GameChallenges
            .Where(c => c.Id == id && c.GameId == gameId).FirstOrDefaultAsync(token);

    public Task LoadFlags(GameChallenge challenge, CancellationToken token = default) =>
        Context.Entry(challenge).Collection(c => c.Flags).LoadAsync(token);

    public Task<GameChallenge[]> GetChallenges(int gameId, CancellationToken token = default) =>
        Context.GameChallenges.Where(c => c.GameId == gameId).OrderBy(c => c.Id).ToArrayAsync(token);

    public Task<GameChallenge[]> GetChallengesWithTrafficCapturing(int gameId, CancellationToken token = default) =>
        Context.GameChallenges.IgnoreAutoIncludes().Where(c => c.GameId == gameId && c.EnableTrafficCapture)
            .ToArrayAsync(token);

    public async Task RemoveChallenge(GameChallenge challenge, bool save = true, CancellationToken token = default)
    {
        await blobRepository.DeleteAttachment(challenge.Attachment, token);

        await LoadFlags(challenge, token);

        // only dynamic attachment challenge's flag contexts have attachment
        if (challenge.Type == ChallengeType.DynamicAttachment)
            foreach (var flag in challenge.Flags)
                await blobRepository.DeleteAttachment(flag.Attachment, token);

        Context.RemoveRange(challenge.Flags);
        Context.Remove(challenge);

        if (save)
            await SaveAsync(token);
    }

    public async Task<TaskStatus> RemoveFlag(GameChallenge challenge, int flagId, CancellationToken token = default)
    {
        var flag = await Context.FlagContexts
            .FirstOrDefaultAsync(f => f.Challenge == challenge && f.Id == flagId, token);

        if (flag is null)
            return TaskStatus.NotFound;

        await blobRepository.DeleteAttachment(flag.Attachment, token);

        Context.Remove(flag);

        await SaveAsync(token);

        if (await Context.FlagContexts.CountAsync(f => f.Challenge == challenge, token) == 0)
        {
            challenge.IsEnabled = false;
            await SaveAsync(token);
        }

        return TaskStatus.Success;
    }

    public async Task<bool> RecalculateAcceptedCount(Game game, CancellationToken token = default)
    {
        var trans = await Context.Database.BeginTransactionAsync(token);

        try
        {
            var query = Context.GameInstances.AsNoTracking()
                .IgnoreAutoIncludes()
                .Include(i => i.Participation)
                .Include(i => i.Challenge)
                .Where(i => i.Challenge.GameId == game.Id && i.IsSolved &&
                            i.Participation.Status == ParticipationStatus.Accepted)
                .GroupBy(i => i.ChallengeId)
                .Select(g => new { ChallengeId = g.Key, Count = g.Count() });

            await Context.GameChallenges.IgnoreAutoIncludes()
                .Where(c => query.Any(r => r.ChallengeId == c.Id))
                .ExecuteUpdateAsync(
                    setter =>
                        setter.SetProperty(
                            c => c.AcceptedCount,
                            c => query.First(r => r.ChallengeId == c.Id).Count),
                    token);

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
        var attachment = model.ToAttachment(await blobRepository.GetBlobByHash(model.FileHash, token));

        await blobRepository.DeleteAttachment(challenge.Attachment, token);

        if (attachment is not null)
            await Context.AddAsync(attachment, token);

        challenge.Attachment = attachment;

        await SaveAsync(token);
    }
}
