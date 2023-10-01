using GZCTF.Models.Request.Edit;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class GameChallengeRepository(AppDbContext context, IFileRepository fileRepository) : RepositoryBase(context),
    IGameChallengeRepository
{
    public async Task AddFlags(GameChallenge gameChallenge, FlagCreateModel[] models, CancellationToken token = default)
    {
        foreach (FlagCreateModel model in models)
        {
            Attachment? attachment = model.AttachmentType == FileType.None
                ? null
                : new()
                {
                    Type = model.AttachmentType,
                    LocalFile = await fileRepository.GetFileByHash(model.FileHash, token),
                    RemoteUrl = model.RemoteUrl
                };

            gameChallenge.Flags.Add(new() { Flag = model.Flag, Challenge = gameChallenge, Attachment = attachment });
        }

        await SaveAsync(token);
    }

    public async Task<GameChallenge> CreateChallenge(Game game, GameChallenge gameChallenge, CancellationToken token = default)
    {
        await context.AddAsync(gameChallenge, token);
        game.Challenges.Add(gameChallenge);
        await SaveAsync(token);
        return gameChallenge;
    }

    public async Task<bool> EnsureInstances(GameChallenge gameChallenge, Game game, CancellationToken token = default)
    {
        await context.Entry(gameChallenge).Collection(c => c.Teams).LoadAsync(token);
        await context.Entry(game).Collection(g => g.Participations).LoadAsync(token);

        var update = game.Participations.Aggregate(false,
            (current, participation) => gameChallenge.Teams.Add(participation) || current);

        await SaveAsync(token);

        return update;
    }

    public Task<GameChallenge?> GetChallenge(int gameId, int id, bool withFlag = false, CancellationToken token = default)
    {
        IQueryable<GameChallenge> challenges = context.GameChallenges
            .Where(c => c.Id == id && c.GameId == gameId);

        if (withFlag)
            challenges = challenges.Include(e => e.Flags);

        return challenges.FirstOrDefaultAsync(token);
    }

    public Task<GameChallenge[]> GetChallenges(int gameId, CancellationToken token = default) =>
        context.GameChallenges.Where(c => c.GameId == gameId).OrderBy(c => c.Id).ToArrayAsync(token);

    public Task<GameChallenge[]> GetChallengesWithTrafficCapturing(int gameId, CancellationToken token = default) =>
        context.GameChallenges.IgnoreAutoIncludes().Where(c => c.GameId == gameId && c.EnableTrafficCapture)
            .ToArrayAsync(token);

    public Task<bool> VerifyStaticAnswer(GameChallenge gameChallenge, string flag, CancellationToken token = default) =>
        context.Entry(gameChallenge).Collection(e => e.Flags).Query().AnyAsync(f => f.Flag == flag, token);

    public async Task RemoveChallenge(GameChallenge gameChallenge, CancellationToken token = default)
    {
        await DeleteAllAttachment(gameChallenge, true, token);

        context.Remove(gameChallenge);
        await SaveAsync(token);
    }

    public async Task<TaskStatus> RemoveFlag(GameChallenge gameChallenge, int flagId, CancellationToken token = default)
    {
        FlagContext? flag = gameChallenge.Flags.FirstOrDefault(f => f.Id == flagId);

        if (flag is null)
            return TaskStatus.NotFound;

        await DeleteAttachment(flag.Attachment, token);

        context.Remove(flag);

        if (gameChallenge.Flags.Count == 0)
            gameChallenge.IsEnabled = false;

        await SaveAsync(token);

        return TaskStatus.Success;
    }

    public async Task UpdateAttachment(GameChallenge gameChallenge, AttachmentCreateModel model,
        CancellationToken token = default)
    {
        Attachment? attachment = model.AttachmentType == FileType.None
            ? null
            : new()
            {
                Type = model.AttachmentType,
                LocalFile = await fileRepository.GetFileByHash(model.FileHash, token),
                RemoteUrl = model.RemoteUrl
            };

        await DeleteAllAttachment(gameChallenge, false, token);

        if (attachment is not null)
            await context.AddAsync(attachment, token);

        gameChallenge.Attachment = attachment;

        await SaveAsync(token);
    }

    internal async Task DeleteAllAttachment(GameChallenge gameChallenge, bool purge = false, CancellationToken token = default)
    {
        await DeleteAttachment(gameChallenge.Attachment, token);

        if (purge && gameChallenge.Type == ChallengeType.DynamicAttachment)
        {
            foreach (FlagContext flag in gameChallenge.Flags)
                await DeleteAttachment(flag.Attachment, token);

            context.RemoveRange(gameChallenge.Flags);
        }
    }

    internal async Task DeleteAttachment(Attachment? attachment, CancellationToken token = default)
    {
        if (attachment is null)
            return;

        if (attachment.Type == FileType.Local && attachment.LocalFile is not null)
            await fileRepository.DeleteFile(attachment.LocalFile, token);

        context.Remove(attachment);
    }
}