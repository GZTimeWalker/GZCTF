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

    public async Task<int> AddFlags(Challenge challenge, FlagCreateModel[] models, CancellationToken token = default)
    {
        var flags = from model in models
                    select new FlagContext()
                    {
                        Flag = model.Flag,
                        AttachmentType = !string.IsNullOrEmpty(model.FileHash) ? FileType.Local :
                                         !string.IsNullOrEmpty(model.Url) ? FileType.Remote : FileType.None,
                        Challenge = challenge,
                        LocalFile = string.IsNullOrEmpty(model.FileHash) ? null : context.Files.Single(x => x.Hash == model.FileHash),
                        RemoteUrl = model.Url
                    };

        challenge.Flags.AddRange(flags);

        context.Update(challenge);
        await context.SaveChangesAsync(token);

        return flags.Count();
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

    public Task RemoveChallenge(Challenge challenge, CancellationToken token = default)
    {
        context.Remove(challenge);
        return context.SaveChangesAsync(token);
    }

    public async Task<TaskStatus> RemoveFlag(Challenge challenge, int flagId, CancellationToken token = default)
    {
        var flag = challenge.Flags.FirstOrDefault(f => f.Id == flagId);

        if (flag is null)
            return TaskStatus.NotFound;

        if (flag.AttachmentType == FileType.Local && flag.LocalFile is not null)
        {
            var res = await fileRepository.DeleteFileByHash(flag.LocalFile.Hash, token);
            if (res != TaskStatus.Success)
                return res;
        }

        context.Remove(flag);
        await context.SaveChangesAsync(token);

        return TaskStatus.Success;
    }

    public Task<bool> VerifyStaticAnswer(Challenge challenge, string flag, CancellationToken token = default)
        => context.Entry(challenge).Collection(e => e.Flags).Query().AnyAsync(f => f.Flag == flag, token);
}