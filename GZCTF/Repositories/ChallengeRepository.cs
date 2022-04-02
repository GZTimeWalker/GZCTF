using CTFServer.Models;
using CTFServer.Models.Request.Edit;
using CTFServer.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace CTFServer.Repositories;

public class ChallengeRepository : RepositoryBase, IChallengeRepository
{
    public ChallengeRepository(AppDbContext _context) : base(_context) { }

    public async Task<Challenge> CreateChallenge(Game game, ChallengeInfoModel model, CancellationToken token = default)
    {
        Challenge challenge = new()
        {
            Type = model.Type,
            Title = model.Title,
            Content = model.Content,
            Answer = model.Answer,
            Tag = model.Tag,
            Hints = model.Hints,
            ContainerImage = model.ContainerImage,
            MemoryLimit = model.MemoryLimit ?? 64,
            CPUCount = model.CPUCount ?? 1,
            ContainerExposePort = model.ContainerExposePort ?? 80,
            OriginalScore = model.OriginalScore,
            MinScore = model.MinScore,
            ExpectMaxCount = model.ExpectMaxCount,
            AwardCount = model.AwardCount,
            FileName = model.FileName ?? "attachment",
        };

        await context.AddAsync(challenge, token);

        challenge.Flags = model.Flags.Select(item => new FlagContext()
            {
                Flag = item.Flag,
                AttachmentType = !string.IsNullOrEmpty(item.FileHash) ? FileType.Local :
                                !string.IsNullOrEmpty(item.Url) ? FileType.Remote : FileType.None,
                Challenge = challenge,
                LocalFile = string.IsNullOrEmpty(item.FileHash) ? null : context.Files.Single(x => x.Hash == item.FileHash),
                Url = item.Url
            }).ToList();

        context.Update(challenge);
        await context.SaveChangesAsync(token);

        return challenge;
    }

    public Task<List<Challenge>> GetChallenges(int count = 100, int skip = 0, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public async Task<List<Challenge>> GetChallenges(Game game, CancellationToken token = default)
    {
        await context.Entry(game).Collection(e => e.Challenges).LoadAsync(token);

        return game.Challenges;
    }

    public Task<bool> VerifyStaticAnswer(Challenge challenge, string flag, CancellationToken token = default)
        => context.Entry(challenge).Collection(e => e.Flags).Query().AnyAsync(f => f.Flag == flag, token);
}
