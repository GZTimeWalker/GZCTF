using CTFServer.Models.Data;
using CTFServer.Models.Request.Edit;
using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Repositories;

public class ChallengeRepository : RepositoryBase, IChallengeRepository
{
    private readonly IFileRepository fileRepository;

    public ChallengeRepository(AppDbContext _context, IFileRepository _fileRepository) : base(_context)
    {
        fileRepository = _fileRepository;
    }

    public async Task AddFlags(Challenge challenge, FlagCreateModel[] models, CancellationToken token = default)
    {
        foreach (var model in models)
        {
            Attachment? attachment = model.AttachmentType == FileType.None ? null : new()
            {
                Type = model.AttachmentType,
                LocalFile = await fileRepository.GetFileByHash(model.FileHash, token),
                RemoteUrl = model.RemoteUrl
            };

            challenge.Flags.Add(new()
            {
                Flag = model.Flag,
                Challenge = challenge,
                Attachment = attachment
            });
        }

        await UpdateAsync(challenge, token);
    }

    public async Task<Challenge> CreateChallenge(Game game, Challenge challenge, CancellationToken token = default)
    {
        await context.AddAsync(challenge, token);
        game.Challenges.Add(challenge);
        await context.SaveChangesAsync(token);
        return challenge;
    }

    public Task<Challenge?> GetChallenge(int gameId, int id, bool withFlag = false, CancellationToken token = default)
    {
        var challenges = context.Challenges.Where(c => c.Id == id && c.GameId == gameId);

        if (withFlag)
            challenges = challenges.Include(e => e.Flags);

        return challenges.FirstOrDefaultAsync(token);
    }

    public Task<Challenge[]> GetChallenges(int gameId, CancellationToken token = default)
        => context.Challenges.Where(c => c.GameId == gameId).OrderBy(c => c.Id).ToArrayAsync(token);

    public async Task RemoveChallenge(Challenge challenge, CancellationToken token = default)
    {
        if (challenge.Type == ChallengeType.DynamicContainer)
        {
            foreach (var flag in challenge.Flags)
            {
                if (flag.Attachment is not null &&
                    flag.Attachment.Type == FileType.Local &&
                    flag.Attachment.LocalFile is not null)
                {
                    await fileRepository.DeleteFileByHash(
                        flag.Attachment.LocalFile.Hash, token);
                }

                context.Remove(flag);
            }
        }
        else if (challenge.Attachment is not null &&
                 challenge.Attachment.Type == FileType.Local &&
                 challenge.Attachment.LocalFile is not null)
        {
            await fileRepository.DeleteFileByHash(
                challenge.Attachment.LocalFile.Hash, token);
        }

        context.Remove(challenge);
        await context.SaveChangesAsync(token);
    }

    public async Task<TaskStatus> RemoveFlag(Challenge challenge, int flagId, CancellationToken token = default)
    {
        var flag = challenge.Flags.FirstOrDefault(f => f.Id == flagId);

        if (flag is null)
            return TaskStatus.NotFound;

        if (flag.Attachment is not null &&
            flag.Attachment.Type == FileType.Local &&
            flag.Attachment.LocalFile is not null)
        {
            await fileRepository.DeleteFileByHash(
                flag.Attachment.LocalFile.Hash, token);
        }

        context.Remove(flag);
        await context.SaveChangesAsync(token);

        return TaskStatus.Success;
    }

    public async Task UpdateAttachment(Challenge challenge, AttachmentCreateModel model, CancellationToken token = default)
    {
        Attachment? attachment = model.AttachmentType == FileType.None ? null : new()
        {
            Type = model.AttachmentType,
            LocalFile = await fileRepository.GetFileByHash(model.FileHash, token),
            RemoteUrl = model.RemoteUrl
        };

        if (challenge.Attachment is not null)
        {
            if (challenge.Attachment.Type == FileType.Local &&
                challenge.Attachment.LocalFile is not null)
            {
                await fileRepository.DeleteFileByHash(
                    challenge.Attachment.LocalFile.Hash, token);
            }

            context.Remove(challenge.Attachment);
        }

        challenge.Attachment = attachment;

        await UpdateAsync(challenge, token);
    }

    public Task<bool> VerifyStaticAnswer(Challenge challenge, string flag, CancellationToken token = default)
        => context.Entry(challenge).Collection(e => e.Flags).Query().AnyAsync(f => f.Flag == flag, token);
}